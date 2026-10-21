using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJam2026
{
    // Fit the authored 16:9 composition without moving the camera or changing its lens.
    public sealed class GameViewport : MonoBehaviour
    {
        private readonly List<Camera> cameras = new List<Camera>();
        private readonly List<CanvasFrame> frames = new List<CanvasFrame>();
        private readonly RectTransform[] bars = new RectTransform[4];
        private Canvas barsCanvas;
        private bool discover = true;

        private sealed class CanvasFrame
        {
            public Canvas Canvas;
            public RectTransform Content;
            public Vector2 Reference;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (FindFirstObjectByType<GameViewport>() != null) return;
            var owner = new GameObject("16 by 9 game viewport");
            DontDestroyOnLoad(owner);
            owner.AddComponent<GameViewport>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            Canvas.willRenderCanvases += RefreshLayout;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            Canvas.willRenderCanvases -= RefreshLayout;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode) => discover = true;

        // Runs after Start, including HUDs created by GameSession and ShipPanelNavigation.
        private void LateUpdate()
        {
            if (barsCanvas == null) CreateBars();
            if (discover)
            {
                discover = false;
                DiscoverScene();
            }
            RefreshLayout();
        }

        public static Rect Fit(Rect available)
        {
            float width = Mathf.Min(available.width, available.height * 16f / 9f);
            float height = width * 9f / 16f;
            return new Rect(available.center.x - width * .5f,
                available.center.y - height * .5f, width, height);
        }

        private void DiscoverScene()
        {
            cameras.Clear();
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (camera.targetTexture == null && camera.targetDisplay == 0) cameras.Add(camera);

            frames.RemoveAll(frame => frame.Canvas == null);
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas == barsCanvas || !canvas.isRootCanvas ||
                    canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.targetDisplay != 0 ||
                    frames.Exists(frame => frame.Canvas == canvas)) continue;
                var scaler = canvas.GetComponent<CanvasScaler>();
                var reference = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                    ? scaler.referenceResolution : new Vector2(1920, 1080);
                var content = new GameObject("16 by 9 content", typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(canvas.transform, false);
                content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f);
                content.sizeDelta = reference;
                // Preserve each authored child's anchors, offsets and sibling order.
                for (int i = canvas.transform.childCount - 2; i >= 0; i--)
                {
                    var child = canvas.transform.GetChild(i);
                    child.SetParent(content, false);
                    child.SetAsFirstSibling();
                }
                frames.Add(new CanvasFrame { Canvas = canvas, Content = content, Reference = reference });
            }
        }

        private void CreateBars()
        {
            var owner = new GameObject("Viewport black bars", typeof(RectTransform),
                typeof(Canvas), typeof(GraphicRaycaster));
            owner.transform.SetParent(transform, false);
            barsCanvas = owner.GetComponent<Canvas>();
            barsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            barsCanvas.sortingOrder = short.MaxValue;
            for (int i = 0; i < bars.Length; i++)
            {
                var image = new GameObject("Black bar " + i, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(owner.transform, false);
                image.color = Color.black;
                image.raycastTarget = true;
                bars[i] = image.rectTransform;
                bars[i].anchorMin = bars[i].anchorMax = bars[i].pivot = Vector2.zero;
            }
        }

        private void RefreshLayout()
        {
            if (Screen.width <= 0 || Screen.height <= 0 || barsCanvas == null) return;
            Rect available = Application.isMobilePlatform ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
            if (available.width <= 0 || available.height <= 0) return;
            Rect viewport = Fit(available);
            var normalized = new Rect(viewport.x / Screen.width, viewport.y / Screen.height,
                viewport.width / Screen.width, viewport.height / Screen.height);
            foreach (var camera in cameras)
                if (camera != null && camera.targetTexture == null) camera.rect = normalized;
            foreach (var frame in frames)
            {
                if (frame.Canvas == null || frame.Content == null) continue;
                float canvasScale = Mathf.Max(.0001f, frame.Canvas.scaleFactor);
                float scale = Mathf.Min(viewport.width / frame.Reference.x, viewport.height / frame.Reference.y);
                frame.Content.localScale = Vector3.one * (scale / canvasScale);
                frame.Content.anchoredPosition = (viewport.center - new Vector2(Screen.width, Screen.height) * .5f) / canvasScale;
            }
            SetBar(0, new Rect(0, 0, viewport.xMin, Screen.height));
            SetBar(1, new Rect(viewport.xMax, 0, Screen.width - viewport.xMax, Screen.height));
            SetBar(2, new Rect(viewport.xMin, 0, viewport.width, viewport.yMin));
            SetBar(3, new Rect(viewport.xMin, viewport.yMax, viewport.width, Screen.height - viewport.yMax));
        }

        private void SetBar(int index, Rect rect)
        {
            bars[index].anchoredPosition = rect.position;
            bars[index].sizeDelta = rect.size;
        }
    }
}
