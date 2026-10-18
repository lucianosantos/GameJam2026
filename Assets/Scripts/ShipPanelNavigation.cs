using UnityEngine;
using UnityEngine.UI;

// The return button is available while inspecting a minigame or the status monitor.
[DisallowMultipleComponent, RequireComponent(typeof(CameraController))]
public sealed class ShipPanelNavigation : MonoBehaviour
{
    [SerializeField] private string backLabel = "Back to panel";
    private CameraController controller;
    private RectTransform canvasRect;
    private GameObject backButton;

    private void Start()
    {
        controller = GetComponent<CameraController>();
        var canvasObject = new GameObject("ShipPanelNavigationHUD", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasRect = canvasObject.GetComponent<RectTransform>();
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        var back = Box("Back to panel", canvasRect);
        back.anchorMin = back.anchorMax = back.pivot = new Vector2(0, 1);
        back.anchoredPosition = new Vector2(28, -28);
        back.sizeDelta = new Vector2(290, 60);
        backButton = back.gameObject;
        var background = backButton.AddComponent<Image>();
        background.color = new Color(.025f, .08f, .11f, .96f);
        var button = backButton.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(controller.ReturnToOverview);
        // Mouse navigation should not leave a selected button that minigame keys can activate.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var label = Label(backLabel, back, 26);
        label.rectTransform.sizeDelta = back.sizeDelta;
        var shortcut = Label("Esc", back, 18);
        shortcut.rectTransform.anchoredPosition = new Vector2(0, -48);
        shortcut.rectTransform.sizeDelta = new Vector2(290, 28);
        shortcut.color = new Color(.7f, .8f, .83f);

        Refresh();
    }

    private void OnEnable()
    {
        if (canvasRect != null) canvasRect.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (canvasRect != null) canvasRect.gameObject.SetActive(false);
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (controller == null || canvasRect == null) return;
        backButton.SetActive(!controller.IsGameOver && controller.ViewState == CameraViewState.Minigame);
    }

    private static RectTransform Box(string title, Transform parent)
    {
        var rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Text Label(string text, Transform parent, int size)
    {
        var label = Box(text, parent).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.text = text;
        label.color = new Color(.8f, .96f, 1f);
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }
}
