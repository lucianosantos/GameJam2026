using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GameJam2026;
using GameJam2026.Levers;
using GameJam2026.SymbolPassword;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Scene migration kept explicit so it never runs automatically on import.
public static class RequestedAdjustmentsSetup
{
    private const string ScenePath = "Assets/Scenes/ControllerScene.unity";
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static object Read(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);

    [MenuItem("Tools/Ship/Apply Requested Puzzle Adjustments %#F9")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var levers = new SerializedObject(Object.FindFirstObjectByType<LeversBoard>());
        var positions = levers.FindProperty("correctPositions");
        int[] solution = { 4, 4, 2 };
        for (int i = 0; i < 3; i++) positions.GetArrayElementAtIndex(i).intValue = solution[i];
        levers.ApplyModifiedPropertiesWithoutUndo();

        var password = new SerializedObject(Object.FindFirstObjectByType<SymbolPasswordBoard>());
        var monitor = (Image)password.FindProperty("monitor").objectReferenceValue;
        var slots = password.FindProperty("displaySlots");
        for (int i = 0; i < 5; i++)
        {
            var slot = (Image)slots.GetArrayElementAtIndex(i).objectReferenceValue;
            Check(slot.transform.parent == monitor.transform, "Password symbols belong to Monitor");
            string name = "Character underline " + (i + 1);
            var existing = monitor.transform.Find(name);
            var bar = existing != null ? existing.GetComponent<Image>() :
                new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            bar.transform.SetParent(monitor.transform, false);
            bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(.5f, .5f);
            bar.rectTransform.sizeDelta = new Vector2(.030f, .002f);
            bar.rectTransform.anchoredPosition3D = new Vector3(slot.rectTransform.anchoredPosition.x, -.023f, -.036f);
            bar.color = Color.white;
            bar.raycastTarget = false;
        }

        var status = Object.FindFirstObjectByType<StatusMonitor>();
        var content = status.transform.Find("Display content");
        var statusData = new SerializedObject(status);
        var lockLabel = (Text)statusData.FindProperty("names").GetArrayElementAtIndex(4).objectReferenceValue;
        var oldPrompt = content.Find("Completion prompt");
        var prompt = oldPrompt != null ? oldPrompt.GetComponent<Text>() : Object.Instantiate(lockLabel, content);
        prompt.name = "Completion prompt";
        prompt.text = "All systems check. Press the button!";
        prompt.rectTransform.anchoredPosition = new Vector2(0, lockLabel.rectTransform.anchoredPosition.y - 74);
        prompt.rectTransform.sizeDelta = new Vector2(960, 76);
        prompt.fontSize = 46;
        prompt.alignment = TextAnchor.MiddleCenter;
        prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
        prompt.verticalOverflow = VerticalWrapMode.Truncate;
        statusData.FindProperty("completionPrompt").objectReferenceValue = prompt;
        statusData.ApplyModifiedPropertiesWithoutUndo();
        status.Refresh();

        var symbol = GameObject.Find("Symbol_fio_musica");
        symbol.GetComponent<MeshFilter>().sharedMesh = GameObject.Find("Symbol_fio_x").GetComponent<MeshFilter>().sharedMesh;
        symbol.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Art/Minigames/Wires/Materials/Symbol_fio_musica.mat");

        var session = Object.FindFirstObjectByType<GameSession>();
        var sessionData = new SerializedObject(session);
        var geometry = GameObject.Find("ShipPanel").GetComponentsInChildren<MeshRenderer>()
            .Concat(GameObject.Find("Minigames").GetComponentsInChildren<MeshRenderer>())
            .Concat(status.GetComponentsInChildren<MeshRenderer>()).Where(r => r.enabled).Distinct().ToArray();
        var references = sessionData.FindProperty("shipGeometry"); references.arraySize = geometry.Length;
        for (int i = 0; i < geometry.Length; i++) references.GetArrayElementAtIndex(i).objectReferenceValue = geometry[i];
        sessionData.FindProperty("finalPlanetClearance").floatValue = .5f;
        sessionData.ApplyModifiedPropertiesWithoutUndo();
        // Keep the final button in the same view as its new instruction.
        var monitorView = GameObject.Find("CameraPositions/StatusMonitor").transform;
        var viewBounds = status.transform.Find("Curved glass").GetComponent<Renderer>().bounds;
        viewBounds.Encapsulate(Object.FindFirstObjectByType<ShipCompletionButton>().GetComponent<Renderer>().bounds);
        float distance = .35f;
        float tangent = Mathf.Tan(Camera.main.fieldOfView * .5f * Mathf.Deg2Rad);
        for (int i = 0; i < 8; i++)
        {
            var corner = Vector3.Scale(viewBounds.extents, new Vector3((i & 1) == 0 ? -1 : 1,
                (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var local = Quaternion.Inverse(monitorView.rotation) * corner;
            distance = Mathf.Max(distance, Mathf.Max(Mathf.Abs(local.x) / (tangent * 16f / 9f),
                Mathf.Abs(local.y) / tangent) * 1.15f - local.z);
        }
        monitorView.position = viewBounds.center - monitorView.forward * distance;
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ResultScreenSetup.Apply();
        Validate();
    }

    public static void Validate()
    {
        var session = Object.FindFirstObjectByType<GameSession>();
        var planet = (Transform)Read(session, "planet");
        var position = planet.position; var scale = planet.localScale;
        var controller = session.GetComponent<CameraController>();
        var report = new List<string>();
        try
        {
            Invoke(session, "Start");
            float duration = (float)Read(session, "initialSeconds");
            float multiplier = (float)Read(session, "finalPlanetScale");
            float initialDiameter = 2 * (float)Read(session, "planetInitialRadius");
            report.Add($"Planet: duration={duration}s, initial diameter={initialDiameter:F4}, final diameter={initialDiameter*multiplier:F4}, diameter growth/s={initialDiameter*(multiplier-1)/duration:F6}, clearance={(float)Read(session, "finalPlanetClearance"):F2}.");
            var geometry = (Renderer[])Read(session, "shipGeometry");
            report.Add("Ship geometry parts: " + geometry.Length);
            for (int t = 0; t <= duration; t++)
            {
                var clock = new CountdownClock(duration); clock.Advance(t);
                session.GetType().GetField("clock", Private).SetValue(session, clock);
                Invoke(session, "UpdateTimeEffects");
                float radius = initialDiameter * .5f * Mathf.Lerp(1, multiplier, t / duration);
                foreach (var part in geometry)
                    Check(Vector3.Distance(planet.position, part.bounds.ClosestPoint(planet.position)) >= radius + .499f,
                        "Planet intersects ship bounds at second " + t + ": " + part.name);
                if (t == 0 || t == 300 || t == 600)
                    StatusMonitorSetup.Capture(Camera.main, "Temp/PlanetAt" + t + ".png");
            }
            report.Add("PASS: every second, 0..600; planet stays at least 0.5 units outside every configured ship bound.");
            Invoke(session, "End", true);
            Check(((Text)Read(session,"resultText")).color == Color.white, "White victory heading");
            Check(((Text)Read(session,"resultMessage")).color == new Color(.48f,.86f,.62f), "Victory message green");
            Check(((Button)Read(session,"replayButton")).GetComponentInChildren<Text>().color == Color.white, "White button text");
            StatusMonitorSetup.Capture(Camera.main, "Temp/AdjustedVictory.png");
            report.Add("PASS: white victory heading and buttons; light green victory message.");
            // Render the complete monitor, then restore the board models through a scene reload below.
            var monitor = Object.FindFirstObjectByType<StatusMonitor>();
            var prompt = (Text)Read(monitor, "completionPrompt");
            Check(prompt != null && !prompt.enabled, "Completion instruction hidden while puzzles are incomplete");
            prompt.enabled = true;
            Canvas.ForceUpdateCanvases();
            var camera = (Camera)Read(monitor,"displayCamera"); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = camera.targetTexture;
            var image = new Texture2D(1024,640,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1024,640),0,0); image.Apply();
            File.WriteAllBytes("Temp/AdjustedMonitor.png",image.EncodeToPNG());
            Object.DestroyImmediate(image); RenderTexture.active = previous;
            report.Add("PASS: completion prompt authored below Lock, gated by all five puzzles.");
            File.WriteAllLines("Temp/RequestedAdjustmentsValidation.txt", report);
            Debug.Log(string.Join("\n", report));
        }
        finally
        {
            planet.position = position; planet.localScale = scale;
            controller.SetTimeShake(0);
            EditorSceneManager.OpenScene(ScenePath);
        }
    }
}

