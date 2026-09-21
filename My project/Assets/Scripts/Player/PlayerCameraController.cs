using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    Transform playerTransform;
    Transform cameraTransform;
    float mouseSensitivity;
    float minLookAngle;
    float maxLookAngle;
    bool lockCursorOnStart;
    Vector3 cameraOffset;
    float cameraSmoothTime;
    float rotationSpeed;
    float cameraYaw;
    float cameraPitch;
    Vector3 cameraFollowVelocity;

    public float Yaw => cameraYaw;

    public void Initialize(
        Transform player,
        Transform camera,
        float sensitivity,
        float minimumLookAngle,
        float maximumLookAngle,
        bool shouldLockCursor,
        Vector3 offset,
        float smoothTime,
        float playerRotationSpeed)
    {
        playerTransform = player;
        cameraTransform = camera;
        mouseSensitivity = sensitivity;
        minLookAngle = minimumLookAngle;
        maxLookAngle = maximumLookAngle;
        lockCursorOnStart = shouldLockCursor;
        cameraOffset = offset;
        cameraSmoothTime = smoothTime;
        rotationSpeed = playerRotationSpeed;

        if (cameraTransform != null)
        {
            Vector3 cameraAngles = cameraTransform.eulerAngles;
            cameraYaw = cameraAngles.y;
            cameraPitch = NormalizeAngle(cameraAngles.x);
        }

        if (lockCursorOnStart)
            SetCursorLocked(true);
    }

    public void HandleLook()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetCursorLocked(false);

        if (Mouse.current == null || cameraTransform == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            SetCursorLocked(true);

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y * mouseSensitivity, minLookAngle, maxLookAngle);
        cameraYaw += mouseDelta.x * mouseSensitivity;
    }

    public void RotatePlayer()
    {
        if (playerTransform == null)
            return;

        Quaternion targetRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        playerTransform.rotation = Quaternion.Slerp(
            playerTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    public void Follow()
    {
        if (cameraTransform == null || playerTransform == null)
            return;

        Quaternion yawRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 desiredPosition = playerTransform.position + yawRotation * cameraOffset;

        cameraTransform.position = Vector3.SmoothDamp(
            cameraTransform.position,
            desiredPosition,
            ref cameraFollowVelocity,
            cameraSmoothTime);
        cameraTransform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
