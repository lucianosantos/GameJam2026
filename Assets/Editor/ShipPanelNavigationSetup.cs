using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShipPanelNavigationSetup
{
    private const string ScenePath = "Assets/Scenes/ControllerScene.unity";

    [MenuItem("Tools/Ship/Assemble Panel Navigation")]
    public static void Assemble()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open ControllerScene first.");
        var panel = GameObject.Find("ShipPanel").transform;
        var positions = GameObject.Find("CameraPositions").transform;
        var controller = UnityEngine.Object.FindFirstObjectByType<CameraController>();
        foreach (var area in UnityEngine.Object.FindObjectsByType<CameraClickArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Undo.SetTransformParent(area.transform, panel, "Move approach area to ShipPanel");

        var target = positions.Find("StatusMonitor");
        if (target == null)
        {
            target = new GameObject("StatusMonitor").transform;
            Undo.RegisterCreatedObjectUndo(target.gameObject, "Add monitor camera position");
            target.SetParent(positions, false);
            var glass = GameObject.Find("StatusMonitor/Curved glass").GetComponent<Renderer>();
            var overview = positions.Find("Overview");
            target.rotation = overview.rotation;
            // Fit the physical display using its projected corners and the gameplay camera lens.
            var camera = controller.GetComponent<Camera>();
            var bounds = glass.bounds;
            float distance = .35f;
            float tangent = Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
            float aspect = 16f / 9f;
            for (int i = 0; i < 8; i++)
            {
                var offset = Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var local = Quaternion.Inverse(target.rotation) * offset;
                distance = Mathf.Max(distance, Mathf.Max(Mathf.Abs(local.x) / (tangent * aspect),
                    Mathf.Abs(local.y) / tangent) * 1.3f - local.z);
            }
            target.position = bounds.center - target.forward * distance;
        }

        var monitorArea = panel.Find("StatusMonitorClickArea");
        if (monitorArea == null)
        {
            var glass = GameObject.Find("StatusMonitor/Curved glass").GetComponent<Renderer>();
            var areaObject = new GameObject("StatusMonitorClickArea");
            Undo.RegisterCreatedObjectUndo(areaObject, "Add monitor approach area");
            areaObject.transform.SetParent(panel, false);
            areaObject.transform.position = glass.bounds.center;
            var collider = areaObject.AddComponent<BoxCollider>();
            collider.size = glass.bounds.size + Vector3.one * .02f;
            var area = areaObject.AddComponent<CameraClickArea>();
            var serialized = new SerializedObject(area);
            serialized.FindProperty("targetPosition").objectReferenceValue = target;
            serialized.FindProperty("cameraController").objectReferenceValue = controller;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        if (controller.GetComponent<ShipPanelNavigation>() == null)
            Undo.AddComponent<ShipPanelNavigation>(controller.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
    }

    [MenuItem("Tools/Validation/Check Panel Navigation")]
    public static void Validate()
    {
        var areas = UnityEngine.Object.FindObjectsByType<CameraClickArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var panel = GameObject.Find("ShipPanel").transform;
        if (areas.Length != 10) throw new Exception("Expected nine existing areas plus the monitor; found " + areas.Length);
        foreach (var area in areas)
        {
            if (area.transform.parent != panel) throw new Exception("Area outside ShipPanel: " + area.name);
            if (area.TargetPosition == null) throw new Exception("Missing camera target: " + area.name);
        }
        if (areas.Select(a => a.TargetPosition).Distinct().Count() != 7)
            throw new Exception("Expected six minigame targets and one monitor target.");
        if (GameObject.Find("CameraPositions/StatusMonitor") == null)
            throw new Exception("Missing monitor camera position.");
        if (Camera.main.GetComponent<ShipPanelNavigation>() == null)
            throw new Exception("Missing navigation HUD.");
        Debug.Log("PASS: all 10 areas directly under ShipPanel; seven targets; monitor camera; navigation HUD.");
    }

    // Batch entry point for assembling and checking the saved scene with the installed editor.
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Assemble();
        MinigameInteractionChecks.Run();
        Directory.CreateDirectory("Temp/PanelNavigation");
        StatusMonitorSetup.Capture(Camera.main, "Temp/PanelNavigation/Overview.png");
        var camera = Camera.main;
        camera.transform.SetPositionAndRotation(GameObject.Find("CameraPositions/StatusMonitor").transform.position,
            GameObject.Find("CameraPositions/StatusMonitor").transform.rotation);
        StatusMonitorSetup.Capture(camera, "Temp/PanelNavigation/Monitor.png");
        File.WriteAllText("Temp/PanelNavigation/Validation.txt", "PASS: scene structure, monitor target, navigation component and minigame focus/return checks.");
    }
}
