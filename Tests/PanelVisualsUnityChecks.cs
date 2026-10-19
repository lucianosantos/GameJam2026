using System;
using System.Linq;
using System.Reflection;
using GameJam2026.Levers;
using GameJam2026.LightSequence;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Run in Play Mode. Exercises the replacement visuals through the existing input components.
public static class PanelVisualsUnityChecks
{
    private static object Read(object o, string name) => o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
    private static void Call(object o, string name) => o.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, null);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Click(GameObject go) => ExecuteEvents.Execute(go,
        new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);

    public static string Run()
    {
        Check(Application.isPlaying, "Play Mode required");
        var controller = Camera.main.GetComponent<CameraController>();
        var duration = controller.GetType().GetField("moveDuration", BindingFlags.Instance | BindingFlags.NonPublic);
        var oldDuration = duration.GetValue(controller);
        var levers = UnityEngine.Object.FindFirstObjectByType<LeversBoard>();
        var lights = UnityEngine.Object.FindFirstObjectByType<LightSequenceBoard>();
        try
        {
            duration.SetValue(controller, 0f);
            controller.MoveTo(GameObject.Find("CameraPositions/Levers").transform);
            levers.RestartPuzzle();
            var handles = (LeverHandle[])Read(levers, "levers");
            foreach (var handle in handles)
            {
                var pivot = (Transform)Read(handle, "movingPivot");
                Vector3 anchor = pivot.position;
                Quaternion previous = pivot.localRotation;
                for (int i = 2; i <= 4; i++)
                {
                    Click(handle.gameObject);
                    Check(handle.Position == i && Vector3.Distance(pivot.position, anchor) < .00001f, "Click rotates around fixed lever pivot");
                    Check(Quaternion.Angle(previous, pivot.localRotation) > 1, "Four distinct lever angles");
                    previous = pivot.localRotation;
                }
                Click(handle.gameObject);
                Check(handle.Position == 1, "Lever cycles through all four positions");
            }
            int[] solution = (int[])Read(levers, "correctPositions");
            for (int i = 0; i < handles.Length; i++) levers.SetPosition(handles[i], solution[i]);
            Check(levers.IsCompleted, "Replacement levers solve the puzzle");

            controller.MoveTo(GameObject.Find("CameraPositions/LightSequence").transform);
            lights.RestartPuzzle();
            var buttons = lights.GetComponentsInChildren<LightSequenceButton>();
            var symbols = buttons.ToDictionary(b => b.ButtonId, b => b.transform.Find("ZodiacSymbol").GetComponent<Image>().sprite);
            foreach (var button in buttons)
            {
                Call(button, "Update");
                var visual = (Transform)Read(button, "visual");
                var legend = button.transform.Find("ZodiacSymbol");
                var rest = visual.localPosition;
                var legendRest = legend.localPosition;
                ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerDownHandler);
                Call(button, "Update");
                Check(visual.localPosition.z > rest.z, "New button depresses");
                Check(((visual.localPosition - rest) - (legend.localPosition - legendRest)).sqrMagnitude < .0000001f, "Symbol follows the button press");
                Call(button, "ResetVisual");
                Check(visual.localPosition == rest && legend.localPosition == legendRest, "Button and symbol return to rest");
            }
            var order = (int[])Read(lights, "correctOrder");
            var block = new MaterialPropertyBlock();
            foreach (int id in order)
            {
                var button = buttons.Single(b => b.ButtonId == id);
                Click(button.gameObject);
                var renderer = (Renderer)Read(button, "lightRenderer");
                renderer.GetPropertyBlock(block);
                Check(button.IsLitCorrect && block.GetColor("_BaseColor") == (Color)Read(button, "correctColor"), "Replacement button lights green");
                Check(button.transform.Find("ZodiacSymbol").GetComponent<Image>().sprite == symbols[id], "Original symbol preserved");
            }
            Check(lights.IsCompleted, "Light sequence completes with replacement buttons");
            Check(GameObject.Find("ShipPanel/CockpitWindow").GetComponentsInChildren<Collider>().Length == 0, "Glass does not intercept clicks");
            return "PASS: fixed lever pivots, four click positions, puzzle completion; all eight new buttons depress and return with their symbols, light green independently and complete the original sequence; glass has no input collider.";
        }
        finally
        {
            levers.RestartPuzzle();
            lights.RestartPuzzle();
            controller.MoveTo(GameObject.Find("CameraPositions/Overview").transform);
            duration.SetValue(controller, oldDuration);
        }
    }
}
