using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 10f;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.1f;
    public float minLookAngle = -80f;
    public float maxLookAngle = 80f;
    public bool lockCursorOnStart = true;

    [Header("Camera Follow")]
    public bool firstPerson = true;
    public float eyeHeight = 1.6f;
    public Vector3 cameraOffset = new Vector3(0f, 2f, -5f);
    public float cameraSmoothTime = 0.15f;

    [Header("Ground Detection")]
    public LayerMask groundLayers = 1 << 3;
    public float groundCheckDistance = 0.35f;
    public float maxGroundAngle = 60f;
    public bool IsGrounded => grounding != null && grounding.IsGrounded;

    [Header("Character Controller")]
    public float gravity = -20f;
    public float groundStickForce = 3f;

    [Header("Keybinds")]
    public KeyCode moveForwardKey = KeyCode.W;
    public KeyCode moveBackwardKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;

    [Header("Exposure")]
    public float exposureRate = 1f;
    public float exposureToChase = 5f;
    public float Exposure { get; private set; }
    public bool IsFullyExposed => Exposure >= exposureToChase;

    [Header("Panic")]
    [Tooltip("Seconds of hiding before panic starts to build.")]
    public float panicGraceSeconds = 4f;
    [Tooltip("Seconds for the panic meter to fill once it starts building.")]
    public float panicFillSeconds = 8f;
    [Tooltip("Seconds for a full panic meter to drain while not hiding.")]
    public float panicRecoverySeconds = 5f;
    [Range(0f, 1f)] public float panicAfterAlert = 0.4f;
    public float Panic { get; private set; }
    public event System.Action<Vector3> PanicAlert;
    public event System.Action<bool> HideChanged;

    [Header("Hiding")]
    public float hideInteractionDistance = 2f;
    public bool hideRenderersWhenHidden = true;
    public bool IsHidden { get; private set; }
    public bool CanHide => !IsHidden && characterController != null && FindNearestHideSpot() != null;
    public bool FlashlightOn => flashlight != null && flashlight.IsOn && !IsHidden;

    CharacterController characterController;
    Collider childCollider;
    Renderer[] renderers;
    PlayerCameraController cameraController;
    PlayerGrounding grounding;
    HideSpot currentHideSpot;
    PlayerFlashlight flashlight;
    float verticalVelocity;
    float hiddenTime;

    void Start()
    {
        LoadKeybinds();

        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("PlayerMovement requires a CharacterController component.", this);
            enabled = false;
            return;
        }

        childCollider = GetComponentInChildren<CapsuleCollider>();
        renderers = GetComponentsInChildren<Renderer>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        cameraController = GetOrAddComponent<PlayerCameraController>();
        grounding = GetOrAddComponent<PlayerGrounding>();

        if (firstPerson)
            SetupFirstPerson();

        cameraController.Initialize(
            transform,
            cameraTransform,
            mouseSensitivity,
            minLookAngle,
            maxLookAngle,
            lockCursorOnStart,
            firstPerson ? new Vector3(0f, eyeHeight, 0f) : cameraOffset,
            firstPerson ? 0f : cameraSmoothTime,
            rotationSpeed);
        grounding.Initialize(
            transform,
            characterController,
            groundLayers,
            groundCheckDistance,
            maxGroundAngle);
    }

    void SetupFirstPerson()
    {
        // Keep the body casting shadows but never draw it from inside.
        foreach (Renderer r in renderers)
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (cameraTransform != null && cameraTransform.TryGetComponent(out Camera playerCamera))
            playerCamera.nearClipPlane = 0.05f;

        if (cameraTransform != null)
        {
            flashlight = GetOrAddComponent<PlayerFlashlight>();
            flashlight.Initialize(cameraTransform, this);
        }
    }

    void Update()
    {
        if (!IsHidden)
            grounding.CheckGrounded();
        cameraController.HandleLook();
        cameraController.RotatePlayer();

        HandleHideInteraction();
        UpdatePanic();
        if (IsHidden)
            return;

        HandleMovement();
    }

    void LateUpdate()
    {
        cameraController.Follow();
    }

    void HandleMovement()
    {
        Vector3 inputDir = Vector3.zero;

        if (IsKeyPressed(moveForwardKey))
            inputDir += Vector3.forward;
        if (IsKeyPressed(moveBackwardKey))
            inputDir -= Vector3.forward;
        if (IsKeyPressed(moveLeftKey))
            inputDir -= Vector3.right;
        if (IsKeyPressed(moveRightKey))
            inputDir += Vector3.right;

        Vector3 moveDirection = Vector3.zero;
        if (inputDir != Vector3.zero && cameraTransform != null)
        {
            inputDir.Normalize();
            Quaternion yawRotation = Quaternion.Euler(0f, cameraController.Yaw, 0f);
            moveDirection = yawRotation * new Vector3(inputDir.x, 0f, inputDir.z);
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -groundStickForce;
        else
            verticalVelocity += gravity * Time.deltaTime;

        Vector3 movement = moveDirection * speed;
        movement.y = verticalVelocity;

        characterController.Move(movement * Time.deltaTime);
    }

    public void IncreaseExposure(float amount)
    {
        Exposure = Mathf.Min(exposureToChase, Exposure + Mathf.Max(0f, amount));
    }

    public void DecreaseExposure(float amount)
    {
        Exposure = Mathf.Max(0f, Exposure - Mathf.Max(0f, amount));
    }

    public void ResetExposure()
    {
        Exposure = 0f;
    }

    // Staying hidden too long builds panic; when it peaks the player gives away the hiding spot.
    void UpdatePanic()
    {
        if (!IsHidden)
        {
            hiddenTime = 0f;
            Panic = Mathf.Max(0f, Panic - Time.deltaTime / Mathf.Max(0.01f, panicRecoverySeconds));
            return;
        }

        hiddenTime += Time.deltaTime;
        if (hiddenTime <= panicGraceSeconds)
            return;

        Panic = Mathf.Min(1f, Panic + Time.deltaTime / Mathf.Max(0.01f, panicFillSeconds));
        if (Panic < 1f)
            return;

        PanicAlert?.Invoke(transform.position);
        Panic = panicAfterAlert;
    }

    void HandleHideInteraction()
    {
        if (Keyboard.current == null || Time.timeScale == 0f || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (IsHidden)
        {
            ExitHideSpot();
            return;
        }

        HideSpot nearestSpot = FindNearestHideSpot();
        if (nearestSpot == null)
            return;

        currentHideSpot = nearestSpot;
        characterController.enabled = false;
        if (childCollider != null)
            childCollider.enabled = false;
        transform.position = currentHideSpot.GetHidePosition();
        SetHiddenVisualState(true);
        IsHidden = true;
        HideChanged?.Invoke(true);
    }

    void ExitHideSpot()
    {
        if (currentHideSpot == null)
            return;

        characterController.enabled = false;
        transform.position = currentHideSpot.GetExitPosition();
        if (childCollider != null)
            childCollider.enabled = true;
        SetHiddenVisualState(false);
        characterController.enabled = true;
        IsHidden = false;
        currentHideSpot = null;
        HideChanged?.Invoke(false);
    }

    void SetHiddenVisualState(bool hidden)
    {
        if (!hideRenderersWhenHidden || renderers == null)
            return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = !hidden;
        }
    }

    HideSpot FindNearestHideSpot()
    {
        HideSpot nearestSpot = null;
        float nearestDistance = hideInteractionDistance;

        foreach (Collider nearbyCollider in Physics.OverlapSphere(transform.position, hideInteractionDistance))
        {
            HideSpot spot = nearbyCollider.GetComponentInParent<HideSpot>();
            if (spot == null)
                continue;

            Vector3 hidePosition = spot.GetHidePosition();
            float distance = Vector3.Distance(transform.position, hidePosition);
            if (distance <= Mathf.Min(nearestDistance, spot.interactionRadius))
            {
                nearestSpot = spot;
                nearestDistance = distance;
            }
        }

        return nearestSpot;
    }

    bool IsKeyPressed(KeyCode key)
    {
        if (Keyboard.current == null)
            return false;

        switch (key)
        {
            case KeyCode.W: return Keyboard.current.wKey.isPressed;
            case KeyCode.S: return Keyboard.current.sKey.isPressed;
            case KeyCode.A: return Keyboard.current.aKey.isPressed;
            case KeyCode.D: return Keyboard.current.dKey.isPressed;
            default: return false;
        }
    }

    void LoadKeybinds()
    {
        moveForwardKey = LoadKey("MoveForwardKey", moveForwardKey);
        moveBackwardKey = LoadKey("MoveBackwardKey", moveBackwardKey);
        moveLeftKey = LoadKey("MoveLeftKey", moveLeftKey);
        moveRightKey = LoadKey("MoveRightKey", moveRightKey);
    }

    KeyCode LoadKey(string prefName, KeyCode defaultKey)
    {
        if (PlayerPrefs.HasKey(prefName))
        {
            string saved = PlayerPrefs.GetString(prefName);
            if (System.Enum.TryParse(saved, out KeyCode parsed))
                return parsed;
        }
        return defaultKey;
    }

    T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
