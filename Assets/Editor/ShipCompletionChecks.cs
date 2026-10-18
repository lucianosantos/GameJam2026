using System;
using System.IO;
using System.Reflection;
using GameJam2026;
using GameJam2026.CelestialLock;
using GameJam2026.Levers;
using GameJam2026.LightSequence;
using GameJam2026.SymbolPassword;
using GameJam2026.Wires;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ShipCompletionChecks
{
    [MenuItem("Tools/Validation/Check Ship Completion In Play Mode")]
    public static void Run()
    {
        Check(Application.isPlaying, "Play Mode required");
        var session = Object.FindFirstObjectByType<GameSession>();
        var button = Object.FindFirstObjectByType<ShipCompletionButton>();
        var controller = Camera.main.GetComponent<CameraController>();
        Write(controller, "moveDuration", 0f);
        controller.ReturnToOverview();
        Check(!session.TryFinishGame() && !session.HasEnded && !button.CanPress, "Early press is rejected");
        var zodiac = Object.FindFirstObjectByType<LightSequenceBoard>();
        var wires = Object.FindFirstObjectByType<WiresBoard>();
        var password = Object.FindFirstObjectByType<SymbolPasswordBoard>();
        var levers = Object.FindFirstObjectByType<LeversBoard>();
        var celestial = Object.FindFirstObjectByType<CelestialLockBoard>();
        var z = new LightSequencePuzzle(new[] { 1, 2 }); z.Press(1); z.Press(2); Write(zodiac, "puzzle", z);
        var w = new WiresPuzzle(new[] { "a", "b", "c", "d", "e", "f", "g", "h" },
            new[] { new WiresPair("a", "b"), new WiresPair("c", "d"), new WiresPair("e", "f"), new WiresPair("g", "h") });
        for (int i = 0; i < 8; i++) w.TryConnect(i, i);
        Write(wires, "puzzle", w);
        var p = new SymbolPasswordPuzzle(new[] { 1 }); p.Enter(1); p.Submit(); Write(password, "puzzle", p);
        var l = new LeversPuzzle(new[] { 2, 2, 2 }); for (int i = 0; i < 3; i++) l.SetPosition(i, 2);
        Write(levers, "puzzle", l);
        Check(!session.TryFinishGame() && !session.HasEnded, "Four of five cannot win");
        var c = new CelestialLockPuzzle(new[] { 0, 0, 0, 0 }, new[] { 0, 0, 0, 0 }); c.Submit(); Write(celestial, "puzzle", c);
        Call(session, "CheckCompletion");
        var monitor = Object.FindFirstObjectByType<StatusMonitor>(); monitor.Refresh();
        Check(((Text)Read(monitor, "completionPrompt")).enabled, "All systems show the final button instruction");
        Check(session.IsReadyToFinish && !session.HasEnded && !controller.IsGameOver, "All puzzles unlock without ending");
        double before = session.RemainingSeconds;
        Write(session, "lastTick", Time.unscaledTimeAsDouble - 1d);
        Call(session, "CheckCompletion");
        Check(session.RemainingSeconds < before - .9d, "Countdown continues while waiting for button");
        Call(button, "LateUpdate");
        var renderer = button.GetComponent<Renderer>();
        var blue = renderer.material.GetColor("_BaseColor");
        Check(blue.b > blue.g * 2 && blue.b > blue.r * 2, "Button is blue rather than cyan");
        var spill = button.GetComponentInChildren<Light>();
        float low = spill.intensity;
        Write(button, "readyAt", Time.unscaledTime - 1f); Call(button, "LateUpdate");
        Check(spill.intensity > low * 2 && low > 0, "Glow breathes without switching off");
        Object.FindFirstObjectByType<StatusMonitor>().Refresh();
        Physics.SyncTransforms();
        var screen = Camera.main.WorldToScreenPoint(renderer.bounds.center);
        Check(ShipCompletionButton.FindAtScreenPoint(Camera.main, screen) == button, "Overview ray reaches cap");
        var area = CameraClickArea.FindAtScreenPoint(Camera.main, screen);
        Check(area == null || !area.TryNavigateAtScreenPoint(Camera.main, screen), "Button has priority over navigation");
        controller.MoveTo(GameObject.Find("CameraPositions/StatusMonitor").transform);
        Physics.SyncTransforms();
        screen = Camera.main.WorldToScreenPoint(renderer.bounds.center);
        Check(screen.z > 0 && screen.x > 0 && screen.x < Camera.main.pixelWidth &&
            screen.y > 0 && screen.y < Camera.main.pixelHeight, $"Button is visible in monitor view: screen={screen}, dimensions={Camera.main.pixelWidth}x{Camera.main.pixelHeight}");
        Check(ShipCompletionButton.FindAtScreenPoint(Camera.main, screen) == button, "Monitor ray reaches cap");
        Capture("Temp/ShipCompletionReady.png");
        // A real screen overlay must prevent activation.
        var blocker = new GameObject("Temporary blocking overlay", typeof(Canvas), typeof(GraphicRaycaster));
        blocker.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var image = new GameObject("Blocker", typeof(RectTransform), typeof(Image)); image.transform.SetParent(blocker.transform, false);
        var rect = image.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Canvas.ForceUpdateCanvases();
        Check(!button.TryPressAtScreenPoint(Camera.main, screen) && !session.HasEnded, "Screen UI blocks the button");
        Object.DestroyImmediate(blocker);
        controller.MoveTo(GameObject.Find("CameraPositions/Levers").transform);
        Check(!button.CanPress, "Unrelated minigame focus blocks button");
        Write(controller, "moveDuration", .6f); controller.ReturnToOverview();
        Check(!button.CanPress, "Camera transition blocks button");
        Write(controller, "moveDuration", 0f);
        controller.MoveTo(GameObject.Find("CameraPositions/StatusMonitor").transform);
        Write(celestial, "puzzle", null); Call(session, "CheckCompletion"); Call(button, "LateUpdate");
        Check(!button.CanPress && !session.IsReadyToFinish && !spill.gameObject.activeInHierarchy, "Puzzle reset removes readiness and glow");
        Write(celestial, "puzzle", c); Call(session, "CheckCompletion"); Call(button, "LateUpdate");
        screen = Camera.main.WorldToScreenPoint(renderer.bounds.center);
        Check(button.TryPressAtScreenPoint(Camera.main, screen) && session.HasWon && session.HasEnded, "Physical press wins");
        Check(controller.IsGameOver && !session.TryFinishGame(), "Win happens once and stops input");
        Check(((Text)Read(session, "resultText")).text == "Congratulations!" &&
            ((Text)Read(session, "resultMessage")).text == "You won the game!", "English victory wording");
        var title = (Text)Read(session, "resultText");
        Check(title.color == Color.white, "Victory heading is white");
        Check(((Text)Read(session, "resultMessage")).color == new Color(.48f, .86f, .62f), "Victory message uses light green");
        Check(((Button)Read(session, "replayButton")).GetComponentInChildren<Text>().text == "Play Again", "Victory replay wording");
        Call(button, "LateUpdate");
        Check(!spill.gameObject.activeInHierarchy, "Ending stops button glow");
        Capture("Temp/ShipCompletionVictory.png");
        // The same completed boards cannot beat an expired deadline in a fresh session.
        var late = new GameObject("Deadline verification", typeof(CameraController), typeof(GameSession));
        var lateSession = late.GetComponent<GameSession>();
        foreach (var field in new[] { "result", "victoryContent", "defeatContent" }) Write(lateSession, field, Read(session, field));
        Call(lateSession, "Start");
        ((CountdownClock)Read(lateSession, "clock")).Advance(600);
        Check(!lateSession.TryFinishGame() && lateSession.HasEnded && !lateSession.HasWon, "Expired deadline loses even with all puzzles solved");
        Check(((Text)Read(lateSession, "resultText")).color == new Color(1f, .33f, .23f) &&
            ((Text)Read(lateSession, "resultMessage")).color == new Color(1f, .33f, .23f), "Defeat text is red");
        Check(((Button)Read(lateSession, "replayButton")).GetComponentInChildren<Text>().text == "Try Again", "Failure keeps Try Again");
        Object.Destroy(late);
        File.WriteAllText("Temp/ShipCompletionValidation.txt", "PASS: Unity Play Mode; incomplete and partial puzzles reject completion; all puzzles unlock without winning; timer continues; smooth blue glow; overview and monitor physical clicks; UI and transition blocking; puzzle reset; victory text and colors; duplicate clicks; deadline still loses.");
        Debug.Log(File.ReadAllText("Temp/ShipCompletionValidation.txt"));
    }

    private static object Read(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Write(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Call(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static void Capture(string path)
    {
        // Include the runtime screen overlay in the camera render for visual review.
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var overlays = Array.FindAll(canvases, canvas => canvas.renderMode == RenderMode.ScreenSpaceOverlay);
        foreach (var canvas in overlays) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main; canvas.planeDistance = .5f; }
        Canvas.ForceUpdateCanvases();
        StatusMonitorSetup.Capture(Camera.main, path);
        foreach (var canvas in overlays) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
    }
}
