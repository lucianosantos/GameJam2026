using System;
using System.IO;
using System.Reflection;
using GameJam2026;
using GameJam2026.Levers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MobileViewportChecks
{
    private const string Running = "MobileViewportChecks.Running";
    private static int frames;
    static MobileViewportChecks()
    {
        if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
    }

    public static void RunBatch()
    {
        ClickAreaUIBlockingChecks.Run();
        CheckFit(new Rect(0, 0, 3088, 1440));
        CheckFit(new Rect(0, 0, 1920, 1080));
        CheckFit(new Rect(0, 0, 1440, 3088));
        CheckFit(new Rect(80, 24, 2920, 1380));
        EditorSceneManager.OpenScene("Assets/Scenes/ControllerScene.unity");
        SessionState.SetBool(Running, true);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
    }

    private static void CheckFit(Rect available)
    {
        Rect fit = GameViewport.Fit(available);
        Check(Mathf.Abs(fit.width / fit.height - 16f / 9f) < .0001f, "16:9 aspect");
        Check(Vector2.Distance(fit.center, available.center) < .001f, "Centered viewport");
        Check(fit.xMin >= available.xMin - .001f && fit.xMax <= available.xMax + .001f &&
            fit.yMin >= available.yMin - .001f && fit.yMax <= available.yMax + .001f, "Viewport inside safe area");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (++frames < 30) return;
        EditorApplication.update -= Tick;
        SessionState.SetBool(Running, false);
        try
        {
            Check(Object.FindFirstObjectByType<GameViewport>() != null, "Viewport bootstraps in Play Mode");
            Canvas.ForceUpdateCanvases();
            var camera = Camera.main;
            Check(Mathf.Abs(camera.pixelRect.width / camera.pixelRect.height - 16f / 9f) < .01f,
                "Gameplay camera has 16:9 viewport");
            int overlays = 0;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.name == "Viewport black bars") continue;
                var content = canvas.transform.Find("16 by 9 content") as RectTransform;
                Check(content != null, "Overlay fitted: " + canvas.name);
                // Activate inactive result canvases for a valid scale factor, then restore them.
                bool active = canvas.gameObject.activeSelf;
                canvas.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                content.GetWorldCorners(corners);
                var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                Check(Vector2.Distance(min, camera.pixelRect.min) < 2f && Vector2.Distance(max, camera.pixelRect.max) < 2f,
                    "UI and camera share the same rectangle: " + canvas.name);
                canvas.gameObject.SetActive(active);
                overlays++;
            }
            Check(overlays >= 3, "Authored UI and runtime HUDs covered");
            var controller = camera.GetComponent<CameraController>();
            var duration = typeof(CameraController).GetField("moveDuration", BindingFlags.Instance | BindingFlags.NonPublic);
            duration.SetValue(controller, 0f);
            var board = Object.FindFirstObjectByType<LeversBoard>();
            var target = (Transform)typeof(LeversBoard).GetField("cameraTarget", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);
            controller.MoveTo(target);
            board.RestartPuzzle();
            var handle = board.GetComponentInChildren<LeverHandle>();
            Check(!((object)handle is IDragHandler) && !((object)handle is IBeginDragHandler) && !((object)handle is IEndDragHandler),
                "No lever drag handlers");
            for (int i = 0; i < 4; i++)
            {
                ExecuteEvents.Execute(handle.gameObject, new PointerEventData(EventSystem.current)
                    { button = PointerEventData.InputButton.Left, pointerId = 42 }, ExecuteEvents.pointerClickHandler);
                Check(handle.Position == (i + 1) % 4 + 1, "Pointer click advances exactly one position");
            }
            Directory.CreateDirectory("Temp/MobileViewportChecks");
            File.WriteAllText("Temp/MobileViewportChecks/Result.txt",
                "PASS: centered 16:9 fit at S23, desktop, portrait and offset safe area; Play Mode bootstrap; " +
                "camera/UI alignment including runtime HUDs and inactive results; lever pointer clicks cycle; no drag handlers. " +
                $"Screen={Screen.width}x{Screen.height}, fitted overlays={overlays}.");
            Debug.Log("PASS: MobileViewportChecks");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
