using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    const float ControlsVisibleSeconds = 8f;
    const float ControlsFadeSeconds = 1.5f;

    Canvas canvas;
    TextMeshProUGUI timerText;
    TextMeshProUGUI exposureText;
    Slider exposureSlider;
    GameObject resultPanel;
    TextMeshProUGUI resultText;
    TextMeshProUGUI controlsText;
    TextMeshProUGUI panicText;
    TextMeshProUGUI alertText;
    TextMeshProUGUI keysText;
    TextMeshProUGUI toastText;
    int lastKeysCollected;
    float toastUntil;
    Slider panicSlider;
    float alertUntil;
    TextMeshProUGUI promptText;
    GameObject promptPanel;
    CanvasGroup controlsGroup;
    float controlsStartTime;
    GameController gameController;
    PlayerMovement playerMovement;
    int lastKeybindHash;

    public void Initialize(GameController controller)
    {
        gameController = controller;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            BindPlayer(player.GetComponent<PlayerMovement>());
        BuildUI();
    }

    void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.PanicAlert -= OnPanicAlert;
    }

    void BindPlayer(PlayerMovement player)
    {
        if (player == null || player == playerMovement)
            return;

        if (playerMovement != null)
            playerMovement.PanicAlert -= OnPanicAlert;

        playerMovement = player;
        playerMovement.PanicAlert += OnPanicAlert;
    }

    void OnPanicAlert(Vector3 position)
    {
        alertUntil = Time.unscaledTime + 3f;
    }

    void Update()
    {
        UpdateExposureUI();
        UpdatePanicUI();
        UpdateControlsUI();
    }

    public void UpdateUI(EscapeChallenge challenge)
    {
        if (challenge == null)
            return;

        UpdateKeysUI(challenge);

        if (challenge.IsRunning)
        {
            timerText.text = $"TIME: {FormatTime(challenge.TimeRemaining)}";
            resultPanel.SetActive(false);
            return;
        }

        if (challenge.HasEscaped)
            ShowResult("YOU WIN", new Color(0.25f, 1f, 0.45f));
        else if (challenge.HasFailed)
            ShowResult(challenge.TimedOut ? "TIME'S UP" : "YOU LOSE", new Color(1f, 0.3f, 0.3f));
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(seconds);
        return $"{total / 60}:{total % 60:00}";
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("GameplayUI");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        timerText = CreateText("Timer", canvas.transform, 36f, TextAlignmentOptions.TopLeft);
        RectTransform timerRect = timerText.rectTransform;
        timerRect.anchorMin = new Vector2(0f, 1f);
        timerRect.anchorMax = new Vector2(0f, 1f);
        timerRect.pivot = new Vector2(0f, 1f);
        timerRect.anchoredPosition = new Vector2(32f, -28f);
        timerRect.sizeDelta = new Vector2(260f, 60f);
        timerText.text = "TIME: --";

        exposureText = CreateText("ExposureText", canvas.transform, 24f, TextAlignmentOptions.TopLeft);
        RectTransform exposureTextRect = exposureText.rectTransform;
        exposureTextRect.anchorMin = new Vector2(0f, 1f);
        exposureTextRect.anchorMax = new Vector2(0f, 1f);
        exposureTextRect.pivot = new Vector2(0f, 1f);
        exposureTextRect.anchoredPosition = new Vector2(32f, -82f);
        exposureTextRect.sizeDelta = new Vector2(260f, 36f);
        exposureText.text = "EXPOSURE: 0%";

        GameObject exposureBarObject = new GameObject("ExposureBar");
        exposureBarObject.transform.SetParent(canvas.transform, false);
        Image exposureBackground = exposureBarObject.AddComponent<Image>();
        exposureBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        RectTransform exposureBarRect = exposureBarObject.GetComponent<RectTransform>();
        exposureBarRect.anchorMin = new Vector2(0f, 1f);
        exposureBarRect.anchorMax = new Vector2(0f, 1f);
        exposureBarRect.pivot = new Vector2(0f, 1f);
        exposureBarRect.anchoredPosition = new Vector2(32f, -116f);
        exposureBarRect.sizeDelta = new Vector2(260f, 18f);

        GameObject exposureFillObject = new GameObject("ExposureFill");
        exposureFillObject.transform.SetParent(exposureBarObject.transform, false);
        Image exposureFill = exposureFillObject.AddComponent<Image>();
        exposureFill.color = new Color(1f, 0.25f, 0.12f, 1f);

        RectTransform exposureFillRect = exposureFillObject.GetComponent<RectTransform>();
        exposureFillRect.anchorMin = Vector2.zero;
        exposureFillRect.anchorMax = Vector2.one;
        exposureFillRect.offsetMin = Vector2.zero;
        exposureFillRect.offsetMax = Vector2.zero;

        exposureSlider = exposureBarObject.AddComponent<Slider>();
        exposureSlider.minValue = 0f;
        exposureSlider.maxValue = 1f;
        exposureSlider.value = 0f;
        exposureSlider.interactable = false;
        exposureSlider.fillRect = exposureFillRect;
        exposureSlider.transition = Selectable.Transition.None;

        BuildPanicUI();
        BuildKeysUI();

        resultPanel = new GameObject("ResultPanel");
        resultPanel.transform.SetParent(canvas.transform, false);
        Image panelImage = resultPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);

        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(500f, 300f);

        resultText = CreateText("ResultText", resultPanel.transform, 64f, TextAlignmentOptions.Center);
        resultText.rectTransform.anchorMin = new Vector2(0.5f, 0.65f);
        resultText.rectTransform.anchorMax = new Vector2(0.5f, 0.65f);
        resultText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        resultText.rectTransform.sizeDelta = new Vector2(460f, 100f);

        CreateResultButton("RestartButton", "RESTART", 0.2f, gameController.RestartLevel);
        CreateResultButton("MenuButton", "MAIN MENU", 0.5f, gameController.LoadMenu);
        CreateResultButton("QuitButton", "QUIT", 0.8f, gameController.QuitGame);

        resultPanel.SetActive(false);
        BuildControlsUI();
    }

    // Without an EventSystem no button ever receives a click.
    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    void CreateResultButton(string objectName, string label, float anchorX, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(resultPanel.transform, false);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.9f);
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(anchorX, 0.22f);
        buttonRect.anchorMax = new Vector2(anchorX, 0.22f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(140f, 56f);

        TextMeshProUGUI buttonText = CreateText("ButtonText", buttonObject.transform, 20f, TextAlignmentOptions.Center);
        buttonText.text = label;
        buttonText.color = Color.black;
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = Vector2.zero;
        buttonText.rectTransform.offsetMax = Vector2.zero;
    }

    void BuildKeysUI()
    {
        keysText = CreateText("KeysText", canvas.transform, 32f, TextAlignmentOptions.Top);
        RectTransform keysRect = keysText.rectTransform;
        keysRect.anchorMin = new Vector2(0.5f, 1f);
        keysRect.anchorMax = new Vector2(0.5f, 1f);
        keysRect.pivot = new Vector2(0.5f, 1f);
        keysRect.anchoredPosition = new Vector2(0f, -28f);
        keysRect.sizeDelta = new Vector2(400f, 50f);
        keysText.text = "KEYS: 0/3";

        toastText = CreateText("KeyToast", canvas.transform, 30f, TextAlignmentOptions.Center);
        toastText.color = new Color(0.9f, 0.8f, 0.45f);
        toastText.rectTransform.anchorMin = new Vector2(0.5f, 0.72f);
        toastText.rectTransform.anchorMax = new Vector2(0.5f, 0.72f);
        toastText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        toastText.rectTransform.sizeDelta = new Vector2(900f, 50f);
        toastText.text = "";
    }

    void UpdateKeysUI(EscapeChallenge challenge)
    {
        if (keysText == null)
            return;

        keysText.text = $"KEYS: {challenge.KeysCollected}/{challenge.KeysRequired}";

        if (challenge.KeysCollected > lastKeysCollected)
        {
            toastUntil = Time.unscaledTime + 2.5f;
            toastText.text = challenge.KeysCollected >= challenge.KeysRequired
                ? "All keys found!"
                : $"Key found ({challenge.KeysCollected}/{challenge.KeysRequired})";
        }

        lastKeysCollected = challenge.KeysCollected;

        if (toastText.text.Length > 0 && Time.unscaledTime >= toastUntil)
            toastText.text = "";
    }

    void BuildPanicUI()
    {
        panicText = CreateText("PanicText", canvas.transform, 24f, TextAlignmentOptions.TopLeft);
        RectTransform textRect = panicText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(32f, -142f);
        textRect.sizeDelta = new Vector2(260f, 36f);
        panicText.text = "PANIC: 0%";

        GameObject barObject = new GameObject("PanicBar");
        barObject.transform.SetParent(canvas.transform, false);
        Image barBackground = barObject.AddComponent<Image>();
        barBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(0f, 1f);
        barRect.pivot = new Vector2(0f, 1f);
        barRect.anchoredPosition = new Vector2(32f, -176f);
        barRect.sizeDelta = new Vector2(260f, 18f);

        GameObject fillObject = new GameObject("PanicFill");
        fillObject.transform.SetParent(barObject.transform, false);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = new Color(1f, 0.8f, 0.2f, 1f);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        panicSlider = barObject.AddComponent<Slider>();
        panicSlider.minValue = 0f;
        panicSlider.maxValue = 1f;
        panicSlider.value = 0f;
        panicSlider.interactable = false;
        panicSlider.fillRect = fillRect;
        panicSlider.transition = Selectable.Transition.None;

        alertText = CreateText("PanicAlert", canvas.transform, 34f, TextAlignmentOptions.Center);
        alertText.color = new Color(1f, 0.8f, 0.2f);
        alertText.rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
        alertText.rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
        alertText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        alertText.rectTransform.sizeDelta = new Vector2(900f, 60f);
        alertText.text = "";
    }

    void UpdatePanicUI()
    {
        if (playerMovement == null || panicSlider == null)
            return;

        float panic = playerMovement.Panic;
        panicSlider.value = panic;
        panicText.text = $"PANIC: {Mathf.RoundToInt(panic * 100f)}%";
        panicText.color = Color.Lerp(Color.white, new Color(1f, 0.8f, 0.2f), panic);

        string alert = Time.unscaledTime < alertUntil ? "You panicked - the enemy heard you!" : "";
        if (alertText.text != alert)
            alertText.text = alert;
    }

    void BuildControlsUI()
    {
        GameObject panel = new GameObject("ControlsPanel");
        panel.transform.SetParent(canvas.transform, false);
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.45f);
        background.raycastTarget = false;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = new Vector2(24f, 24f);
        panelRect.sizeDelta = new Vector2(340f, 215f);

        controlsGroup = panel.AddComponent<CanvasGroup>();
        controlsGroup.blocksRaycasts = false;
        controlsGroup.interactable = false;
        controlsStartTime = Time.unscaledTime;

        controlsText = CreateText("ControlsText", panel.transform, 20f, TextAlignmentOptions.TopLeft);
        controlsText.rectTransform.anchorMin = Vector2.zero;
        controlsText.rectTransform.anchorMax = Vector2.one;
        controlsText.rectTransform.offsetMin = new Vector2(14f, 10f);
        controlsText.rectTransform.offsetMax = new Vector2(-14f, -10f);

        promptPanel = new GameObject("HidePromptPanel");
        promptPanel.transform.SetParent(canvas.transform, false);
        Image promptBackground = promptPanel.AddComponent<Image>();
        promptBackground.color = new Color(0f, 0f, 0f, 0.65f);
        promptBackground.raycastTarget = false;

        RectTransform promptRect = promptPanel.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.22f);
        promptRect.anchorMax = new Vector2(0.5f, 0.22f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.sizeDelta = new Vector2(320f, 64f);

        promptText = CreateText("HidePrompt", promptPanel.transform, 30f, TextAlignmentOptions.Center);
        promptText.rectTransform.anchorMin = Vector2.zero;
        promptText.rectTransform.anchorMax = Vector2.one;
        promptText.rectTransform.offsetMin = Vector2.zero;
        promptText.rectTransform.offsetMax = Vector2.zero;
        promptPanel.SetActive(false);
    }

    void UpdateControlsUI()
    {
        if (controlsText == null)
            return;

        float elapsed = Time.unscaledTime - controlsStartTime;
        controlsGroup.alpha = Mathf.Clamp01(1f - (elapsed - ControlsVisibleSeconds) / ControlsFadeSeconds);

        KeyCode forward = playerMovement != null ? playerMovement.moveForwardKey : KeyCode.W;
        KeyCode left = playerMovement != null ? playerMovement.moveLeftKey : KeyCode.A;
        KeyCode backward = playerMovement != null ? playerMovement.moveBackwardKey : KeyCode.S;
        KeyCode right = playerMovement != null ? playerMovement.moveRightKey : KeyCode.D;

        int hash = (int)forward * 1000003 ^ (int)left * 10007 ^ (int)backward * 101 ^ (int)right;
        if (hash != lastKeybindHash)
        {
            lastKeybindHash = hash;
            controlsText.text =
                "<b>CONTROLS</b>\n" +
                $"{forward} {left} {backward} {right}  -  Move\n" +
                "Mouse  -  Look around\n" +
                "E  -  Hide / Come out\n" +
                "F  -  Flashlight\n" +
                "Esc  -  Pause\n" +
                "<b>GOAL:</b> find all the keys";
        }

        string prompt = "";
        bool gameOver = gameController != null && (gameController.HasWon || gameController.HasLost);
        if (playerMovement != null && !gameOver)
            prompt = playerMovement.IsHidden ? "<b>[E]</b>  Come out" : playerMovement.CanHide ? "<b>[E]</b>  Hide" : "";

        promptPanel.SetActive(prompt.Length > 0);
        if (promptText.text != prompt)
            promptText.text = prompt;
    }

    void UpdateExposureUI()
    {
        if (playerMovement == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                BindPlayer(player.GetComponent<PlayerMovement>());
        }

        if (playerMovement == null || exposureSlider == null)
            return;

        float maximumExposure = Mathf.Max(0.01f, playerMovement.exposureToChase);
        float exposurePercent = Mathf.Clamp01(playerMovement.Exposure / maximumExposure);
        exposureSlider.value = exposurePercent;
        exposureText.text = $"EXPOSURE: {Mathf.RoundToInt(exposurePercent * 100f)}%";
        exposureText.color = Color.Lerp(Color.white, new Color(1f, 0.25f, 0.12f), exposurePercent);
    }

    void ShowResult(string message, Color color)
    {
        resultPanel.SetActive(true);
        resultText.text = message;
        resultText.color = color;
        timerText.text = "TIME: 0:00";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
