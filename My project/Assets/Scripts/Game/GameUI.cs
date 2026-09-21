using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    Canvas canvas;
    TextMeshProUGUI timerText;
    TextMeshProUGUI exposureText;
    Slider exposureSlider;
    GameObject resultPanel;
    TextMeshProUGUI resultText;
    GameController gameController;
    PlayerMovement playerMovement;

    public void Initialize(GameController controller)
    {
        gameController = controller;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();
        BuildUI();
    }

    void Update()
    {
        UpdateExposureUI();
    }

    public void UpdateUI(EscapeChallenge challenge)
    {
        if (challenge == null)
            return;

        if (challenge.IsRunning)
        {
            timerText.text = $"TIME: {Mathf.CeilToInt(challenge.TimeRemaining)}";
            resultPanel.SetActive(false);
            return;
        }

        if (challenge.HasEscaped)
            ShowResult("YOU WIN", new Color(0.25f, 1f, 0.45f));
        else if (challenge.HasFailed)
            ShowResult("YOU LOSE", new Color(1f, 0.3f, 0.3f));
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("GameplayUI");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

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

        GameObject buttonObject = new GameObject("RestartButton");
        buttonObject.transform.SetParent(resultPanel.transform, false);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.9f);
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(gameController.RestartLevel);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.25f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.25f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(220f, 60f);

        TextMeshProUGUI buttonText = CreateText("ButtonText", buttonObject.transform, 26f, TextAlignmentOptions.Center);
        buttonText.text = "RESTART";
        buttonText.color = Color.black;
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = Vector2.zero;
        buttonText.rectTransform.offsetMax = Vector2.zero;

        resultPanel.SetActive(false);
    }

    void UpdateExposureUI()
    {
        if (playerMovement == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerMovement = player.GetComponent<PlayerMovement>();
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
        timerText.text = "TIME: 0";
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
