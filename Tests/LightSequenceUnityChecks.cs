using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using GameJam2026.LightSequence;

// Run with the Unity Pipeline run_script command while ControllerScene is in Play Mode.
// Runtime changes are discarded when leaving Play Mode. No assets are edited.
public static class LightSequenceUnityChecks
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    static void Click(LightSequenceButton button) => ExecuteEvents.Execute(button.gameObject,
        new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = -1 },
        ExecuteEvents.pointerClickHandler);
    static Color LightColor(LightSequenceButton button)
        => RendererColor(button.GetComponentInChildren<Renderer>());
    static Color RendererColor(Renderer renderer)
    {
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        return block.GetColor("_BaseColor");
    }
    static bool Near(Color a, Color b) => ((Vector4)a - (Vector4)b).sqrMagnitude < 0.00001f;

    public static string Run()
    {
        Check(Application.isPlaying, "Requires Play Mode");
        var board = UnityEngine.Object.FindFirstObjectByType<LightSequenceBoard>();
        var buttons = board.GetComponentsInChildren<LightSequenceButton>();
        var camera = Camera.main.GetComponent<CameraController>();
        var target = GameObject.Find("CameraPositions/LightSequence").transform;
        var overview = GameObject.Find("CameraPositions/Overview").transform;
        var siren = GameObject.Find("ShipPanel/Painel/Sirene_LightSequence").GetComponentInChildren<LightSequenceStatusLight>();
        Check(siren != null, "Siren status component exists on the supplied mesh");
        var sirenRenderer = siren.GetComponent<Renderer>();
        Color pending = (Color)Field(siren, "pendingColor").GetValue(siren);
        Color completedColor = (Color)Field(siren, "completedColor").GetValue(siren);
        float duration = (float)Field(camera, "moveDuration").GetValue(camera);
        int completions = 0;
        UnityEngine.Events.UnityAction completed = () => completions++;
        board.OnCompleted.AddListener(completed);
        try
        {
            Check(buttons.Length == 8, "Eight scene buttons configured");
            for (int i = 0; i < 8; i++)
            {
                Check(buttons[i].ButtonId == i + 1 && buttons[i].HasLight, "Unique IDs and prefab Renderer");
                Check(buttons[i].GetComponentInChildren<Renderer>().sharedMaterial.IsKeywordEnabled("_EMISSION"), "Emission enabled");
            }
            var configuredOrder = (int[])Field(board, "correctOrder").GetValue(board);
            int[] expectedOrder = { 8, 2, 1, 4, 6, 7, 3, 5 };
            string[] signs = { "PEIXES", "ARIES", "GEMEOS", "CANCER", "LEAO", "VIRGEM", "SAGITARIO", "CAPRICORNIO" };
            Check(configuredOrder.Length == 8, "Eight-symbol solution");
            for (int i = 0; i < 8; i++)
            {
                Check(configuredOrder[i] == expectedOrder[i], "Decoded manual solution");
                var image = buttons[i].transform.Find("ZodiacSymbol").GetComponent<UnityEngine.UI.Image>();
                Check(image.sprite != null && image.sprite.name == "Zodiac_" + signs[i] && !image.raycastTarget, "Zodiac mapping and raycast passthrough");
                var legends = (Transform[])Field(buttons[i], "legends").GetValue(buttons[i]);
                Check(legends.Length == 1 && legends[0] == image.transform, "Legend follows the key animation");
            }
            var byId = new System.Collections.Generic.Dictionary<int, LightSequenceButton>();
            foreach (var button in buttons) byId.Add(button.ButtonId, button);
            buttons = System.Array.ConvertAll(configuredOrder, id => byId[id]);
            Field(camera, "moveDuration").SetValue(camera, 0f);
            board.RestartPuzzle();
            Check(Near(RendererColor(sirenRenderer), pending), "Siren starts red");
            camera.MoveTo(overview);
            Click(buttons[0]);
            Check(board.Progress == 0, "Overview blocks clicks");
            camera.MoveTo(target);
            Click(buttons[0]); Click(buttons[1]);
            Check(board.Progress == 2 && buttons[0].IsLitCorrect && buttons[1].IsLitCorrect, "Correct prefix turns green");
            Check(Near(RendererColor(sirenRenderer), pending), "Partial progress leaves siren red");
            Check(Near(LightColor(buttons[0]), (Color)Field(buttons[0], "correctColor").GetValue(buttons[0])), "Green applied to Renderer");
            Check(!buttons[2].IsLitCorrect, "Unpressed copy stays off");
            Click(buttons[5]);
            Check(board.Progress == 0 && !board.CanAcceptInput, "Wrong press resets and locks during error");
            foreach (var button in buttons) Check(!button.IsLitCorrect, "All previous correct lights cleared");
            Click(buttons[0]); Check(board.Progress == 0, "Clicks ignored during error");
            board.RestartPuzzle();
            var wrong = buttons[5];
            var flashes = wrong.FlashError();
            int yields = 0;
            while (flashes.MoveNext())
            {
                bool red = yields % 2 == 0;
                var expected = red ? (Color)Field(wrong, "errorColor").GetValue(wrong) : wrong.GetComponentInChildren<Renderer>().sharedMaterial.GetColor("_BaseColor");
                Check(Near(LightColor(wrong), expected), "Flash alternates red and off");
                Check(flashes.Current is WaitForSecondsRealtime, "Flash uses timed unscaled waits");
                yields++;
            }
            Check(yields == 5 && !wrong.IsLitCorrect, "Three red flashes then off");
            Check(Near(LightColor(wrong), wrong.GetComponentInChildren<Renderer>().sharedMaterial.GetColor("_BaseColor")), "Flash ends off");
            foreach (var button in buttons) Click(button);
            Check(board.IsCompleted && board.Progress == 8 && completions == 1, "Correct sequence fires success once");
            Check(Near(RendererColor(sirenRenderer), completedColor), "Completion turns siren green");
            siren.enabled = false;
            siren.enabled = true;
            Check(Near(RendererColor(sirenRenderer), completedColor), "Re-enabled siren restores completed state");
            foreach (var button in buttons) { Check(button.IsLitCorrect, "Completed lights stay green"); Click(button); }
            Check(completions == 1 && !board.CanAcceptInput, "Completion locks further input");
            board.RestartPuzzle();
            Check(!board.IsCompleted && board.Progress == 0, "Restart clears completion");
            Check(Near(RendererColor(sirenRenderer), pending), "Restart restores red siren");
            foreach (var button in buttons) Check(!button.IsLitCorrect, "Restart clears lights");
            Click(buttons[0]); Click(buttons[0]);
            Check(board.Progress == 0 && !board.CanAcceptInput, "Out of order repeated correct button is an error");
            board.enabled = false; board.enabled = true;
            Check(board.CanAcceptInput && board.Progress == 0, "Disabling during error cancels feedback cleanly");
            foreach (var button in buttons) Click(button);
            Check(completions == 2, "Re-enable does not duplicate event listeners");
            return "PASS: sequence, independent button colors, error/reset, unique completion event and siren red initially/partially, green on success, red on restart, and correct state after re-enable. Events dispatched through ExecuteEvents; physical mouse not verified.";
        }
        finally
        {
            board.OnCompleted.RemoveListener(completed);
            board.RestartPuzzle();
            camera.MoveTo(overview);
            Field(camera, "moveDuration").SetValue(camera, duration);
        }
    }
}
