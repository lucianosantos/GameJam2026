using System;
using System.Linq;
using System.Reflection;
using GameJam2026.Wires;
using GameJam2026.SymbolPassword;
using GameJam2026.CelestialLock;
using UnityEngine;

// Run through Pipeline run_script in a fresh Play Mode session, then stop Play Mode.
public static class PanelStatusLightsUnityChecks
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Read(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    static void Write(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static Renderer Lamp(string name) => GameObject.Find("ShipPanel/Painel/" + name).GetComponentInChildren<Renderer>();
    static void ColorIs(string name, bool green)
    {
        var renderer = Lamp(name);
        var status = renderer.GetComponent<PuzzleCompletionLight>();
        if (status != null) status.Refresh();
        var properties = new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties);
        Color color = properties.GetColor("_BaseColor");
        Check(green ? color.g > color.r : color.r > color.g, name + " must be " + (green ? "green" : "red") + ": " + color);
    }
    static void Capture(string view, string suffix)
    {
        var camera = Camera.main;
        var target = GameObject.Find("CameraPositions/" + view).transform;
        var pos = camera.transform.position; var rot = camera.transform.rotation;
        camera.transform.SetPositionAndRotation(target.position, target.rotation);
        StatusMonitorSetup.Capture(camera, "Temp/PanelStatus-" + view + "-" + suffix + ".png");
        camera.transform.SetPositionAndRotation(pos, rot);
    }

    public static string Run()
    {
        Check(Application.isPlaying, "Fresh Play Mode required.");
        var wires = UnityEngine.Object.FindFirstObjectByType<WiresBoard>();
        var password = UnityEngine.Object.FindFirstObjectByType<SymbolPasswordBoard>();
        var celestial = UnityEngine.Object.FindFirstObjectByType<CelestialLockBoard>();
        Check(!wires.IsCompleted && !password.IsCompleted && !celestial.IsCompleted, "Start with incomplete boards.");
        foreach (var name in new[] { "Luz_Alavancas", "Luz_Cadeado", "Luz_Fios", "Luz_Senha" })
        {
            ColorIs(name, false);
            Check(Lamp(name).GetComponentsInChildren<Collider>().Length == 0, "Status lamps must not intercept puzzle clicks.");
        }
        Check(ReferenceEquals(Read(Lamp("Luz_Fios").GetComponent<PuzzleCompletionLight>(), "wiresBoard"), wires), "Wires binding");
        Check(ReferenceEquals(Read(Lamp("Luz_Senha").GetComponent<PuzzleCompletionLight>(), "passwordBoard"), password), "Password binding");
        Check(Lamp("Luz_Cadeado").bounds.center.x > celestial.transform.position.x, "Lock lamp is to the right of its board.");
        Check(Lamp("Luz_Fios").bounds.size.x < Lamp("Luz_Cadeado").bounds.size.x && Lamp("Luz_Cadeado").bounds.size.x < Lamp("Luz_Alavancas").bounds.size.x, "Lamp sizes follow small/intermediate/large sockets.");
        foreach (var view in new[] { "Wires", "SymbolPassword", "CelestialLock" }) Capture(view, "Pending");

        var puzzle = (WiresPuzzle)Read(wires, "puzzle");
        for (int i = 0; i < 8; i++) puzzle.TryConnect(i, i);
        Call(wires, "ReleaseEndpoint");
        Check(!wires.IsCompleted, "Wrong full wiring must stay pending."); ColorIs("Luz_Fios", false);
        for (int i = 0; i < 8; i++) puzzle.Disconnect(i);
        int[] solution = { 7, 0, 5, 4, 1, 3, 2, 6 };
        var sockets = (WiresSocket[])Read(wires, "sockets");
        var cables = (WiresCable[])Read(wires, "cables");
        for (int i = 0; i < 8; i++) { puzzle.TryConnect(i, solution[i]); cables[i / 2].End(i % 2).position = sockets[solution[i]].Position; }
        Call(wires, "ReleaseEndpoint"); wires.RefreshCableGeometry();
        Check(wires.IsCompleted, "Correct wiring completes board."); ColorIs("Luz_Fios", true);
        ColorIs("Luz_Senha", false); ColorIs("Luz_Cadeado", false); ColorIs("Luz_Alavancas", false);
        var indicator = Lamp("Luz_Fios").GetComponent<PuzzleCompletionLight>();
        indicator.enabled = false; indicator.enabled = true; ColorIs("Luz_Fios", true);
        Capture("Wires", "Completed");

        var controller = Camera.main.GetComponent<CameraController>();
        float duration = (float)Read(controller, "moveDuration");
        Write(controller, "moveDuration", 0f);
        try
        {
            controller.MoveTo(GameObject.Find("CameraPositions/SymbolPassword").transform);
            var buttons = (SymbolPasswordButton[])Read(password, "buttons");
            foreach (int digit in (int[])Read(password, "correctOrder")) password.PressKey(buttons.Single(b => b.Action == PasswordKeyAction.Digit && b.Digit == digit));
            ColorIs("Luz_Senha", false);
            password.PressKey(buttons.Single(b => b.Action == PasswordKeyAction.Submit));
            Check(password.IsCompleted, "Submitting the correct password completes board.");
            ColorIs("Luz_Senha", true); ColorIs("Luz_Fios", true); ColorIs("Luz_Cadeado", false);
            Capture("SymbolPassword", "Completed");
            password.RestartPuzzle(); ColorIs("Luz_Senha", false); ColorIs("Luz_Fios", true);

            controller.MoveTo(celestial.CameraTarget);
            float turnDuration = (float)Read(celestial, "turnDuration");
            Write(celestial, "turnDuration", .000001f);
            try
            {
                var correct = (CelestialSymbol[])Read(celestial, "correctOrder");
                for (int i = 0; i < 4; i++) for (int j = 0; celestial.SymbolAt(i) != (int)correct[i] && j < 4; j++)
                { Check(celestial.Advance(i), "Roller accepts turn."); Call(celestial, "Update"); }
                Check(celestial.Submit(), "Correct lock combination submits.");
                ColorIs("Luz_Cadeado", true); ColorIs("Luz_Alavancas", false); ColorIs("Luz_Senha", false);
                Capture("CelestialLock", "Completed");
                celestial.RestartPuzzle(); ColorIs("Luz_Cadeado", false);
            }
            finally { Write(celestial, "turnDuration", turnDuration); }
        }
        finally { Write(controller, "moveDuration", duration); }
        return "PASS: four fitted lamps; independent pending/completion colors; incorrect wiring stays red; wires completion and reactivation; password waits for submit and resets red; relocated lock completes/resets independently; lamps have no click-blocking colliders. Six runtime captures saved.";
    }
}
