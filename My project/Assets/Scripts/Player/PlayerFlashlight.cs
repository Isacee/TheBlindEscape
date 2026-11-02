using UnityEngine;
using UnityEngine.InputSystem;

// Hand-held flashlight: the only reliable light in the dark house. Toggle with F.
public class PlayerFlashlight : MonoBehaviour
{
    public float range = 16f;
    public float spotAngle = 55f;
    public float intensity = 3f;

    public bool IsOn { get; private set; } = true;

    Light spot;
    GameObject viewModel;
    PlayerMovement player;

    public void Initialize(Transform cameraTransform, PlayerMovement owner)
    {
        player = owner;

        viewModel = new GameObject("FlashlightViewModel");
        viewModel.transform.SetParent(cameraTransform, false);
        viewModel.transform.localPosition = new Vector3(0.26f, -0.24f, 0.5f);
        viewModel.transform.localRotation = Quaternion.Euler(2f, -3f, 0f);

        Material metal = CreateMaterial(new Color(0.08f, 0.08f, 0.09f), 0.6f, Color.black);
        Material lens = CreateMaterial(new Color(1f, 0.95f, 0.8f), 0.9f, new Color(1f, 0.9f, 0.6f) * 2f);
        Material sleeve = CreateMaterial(new Color(0.12f, 0.15f, 0.2f), 0.1f, Color.black);
        Material skin = CreateMaterial(new Color(0.71f, 0.55f, 0.42f), 0.2f, Color.black);

        Part(PrimitiveType.Cylinder, "Body", new Vector3(0f, 0f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.065f, 0.11f, 0.065f), metal);
        Part(PrimitiveType.Cylinder, "Head", new Vector3(0f, 0f, 0.12f), new Vector3(90f, 0f, 0f), new Vector3(0.095f, 0.03f, 0.095f), metal);
        Part(PrimitiveType.Cylinder, "Lens", new Vector3(0f, 0f, 0.152f), new Vector3(90f, 0f, 0f), new Vector3(0.08f, 0.004f, 0.08f), lens);
        Part(PrimitiveType.Sphere, "Hand", new Vector3(0f, -0.045f, -0.03f), Vector3.zero, new Vector3(0.1f, 0.09f, 0.12f), skin);
        Part(PrimitiveType.Cube, "Sleeve", new Vector3(0.05f, -0.1f, -0.3f), new Vector3(-8f, -6f, 0f), new Vector3(0.1f, 0.1f, 0.5f), sleeve);

        var lightObject = new GameObject("FlashlightSpot");
        lightObject.transform.SetParent(viewModel.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0f, 0.16f);

        spot = lightObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = range;
        spot.spotAngle = spotAngle;
        spot.innerSpotAngle = spotAngle * 0.5f;
        spot.intensity = intensity;
        spot.color = new Color(1f, 0.94f, 0.8f);
        spot.shadows = LightShadows.Soft;
    }

    void Update()
    {
        if (spot == null)
            return;

        if (Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            IsOn = !IsOn;

        bool hidden = player != null && player.IsHidden;
        viewModel.SetActive(!hidden);
        spot.enabled = IsOn && !hidden;

        // Panic makes the beam stutter.
        float flicker = player != null ? Mathf.InverseLerp(0.5f, 1f, player.Panic) : 0f;
        bool glitch = flicker > 0f && Random.value < flicker * 0.25f;
        spot.intensity = glitch ? intensity * Random.Range(0.1f, 0.6f) : intensity;
    }

    void OnDestroy()
    {
        if (viewModel != null)
            Destroy(viewModel);
    }

    void Part(PrimitiveType type, string partName, Vector3 position, Vector3 euler, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(viewModel.transform, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(euler);
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(part.GetComponent<Collider>());
    }

    static Material CreateMaterial(Color color, float smoothness, Color emission)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);

        if (emission != Color.black)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        return material;
    }
}
