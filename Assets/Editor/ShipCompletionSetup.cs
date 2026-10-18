using System;
using System.Collections.Generic;
using System.Linq;
using GameJam2026;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShipCompletionSetup
{
    [MenuItem("Tools/Ship/Set Up Completion Button")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/ControllerScene.unity")
            scene = EditorSceneManager.OpenScene("Assets/Scenes/ControllerScene.unity");
        // The round cap immediately to the right of the curved status monitor.
        var cap = GameObject.Find("ShipPanel/Painel/CompletionButton");
        if (cap == null) cap = ExtractCap();
        if (cap.GetComponent<Collider>() == null)
        {
            var collider = Undo.AddComponent<MeshCollider>(cap);
            collider.sharedMesh = cap.GetComponent<MeshFilter>().sharedMesh;
        }
        var button = cap.GetComponent<ShipCompletionButton>();
        if (button == null) button = Undo.AddComponent<ShipCompletionButton>(cap);
        var serialized = new SerializedObject(button);
        serialized.FindProperty("session").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<GameSession>();
        serialized.FindProperty("controller").objectReferenceValue = Camera.main.GetComponent<CameraController>();
        serialized.FindProperty("monitorView").objectReferenceValue = GameObject.Find("CameraPositions/StatusMonitor").transform;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Completion button connected to the cap right of the status monitor: " + cap.GetComponent<Renderer>().bounds);
    }

    private static GameObject ExtractCap()
    {
        var source = GameObject.Find("ShipPanel/Painel/Painel_LP").GetComponent<MeshFilter>();
        var mesh = source.sharedMesh;
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        // The cap's upper surface is its own UV island in the artist's combined mesh.
        Vector3 top = new Vector3(.7643855f, -.707385f, -.915506f);
        int seed = Enumerable.Range(0, vertices.Length).OrderBy(i =>
            (source.transform.TransformPoint(vertices[i]) - top).sqrMagnitude).First();
        if (Vector3.Distance(source.transform.TransformPoint(vertices[seed]), top) > .01f)
            throw new InvalidOperationException("Panel geometry changed; inspect the completion cap before extracting it.");
        var island = new HashSet<int> { seed };
        bool changed;
        do
        {
            changed = false;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (!island.Contains(triangles[i]) && !island.Contains(triangles[i + 1]) && !island.Contains(triangles[i + 2])) continue;
                for (int j = 0; j < 3; j++) changed |= island.Add(triangles[i + j]);
            }
        } while (changed);
        if (island.Count != 21) throw new InvalidOperationException("Unexpected cap topology; original mesh preserved.");
        var indices = island.OrderBy(i => i).ToArray();
        var mapping = indices.Select((old, index) => new { old, index }).ToDictionary(x => x.old, x => x.index);
        var capTriangles = new List<int>();
        var panelTriangles = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            bool cap = island.Contains(triangles[i]);
            for (int j = 0; j < 3; j++)
                if (cap) capTriangles.Add(mapping[triangles[i + j]]); else panelTriangles.Add(triangles[i + j]);
        }
        var capMesh = new Mesh { name = "CompletionButtonCap" };
        capMesh.vertices = indices.Select(i => vertices[i]).ToArray();
        capMesh.normals = indices.Select(i => mesh.normals[i]).ToArray();
        capMesh.uv = indices.Select(i => mesh.uv[i]).ToArray();
        capMesh.tangents = indices.Select(i => mesh.tangents[i]).ToArray();
        capMesh.triangles = capTriangles.ToArray();
        capMesh.RecalculateBounds();
        var panelMesh = UnityEngine.Object.Instantiate(mesh);
        panelMesh.name = "PanelWithoutCompletionCap";
        panelMesh.triangles = panelTriangles.ToArray();
        const string folder = "Assets/Art/CompletionButton";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Art", "CompletionButton");
        AssetDatabase.CreateAsset(capMesh, folder + "/CompletionButtonCap.asset");
        AssetDatabase.CreateAsset(panelMesh, folder + "/PanelWithoutCompletionCap.asset");
        Undo.RecordObject(source, "Separate completion button cap");
        source.sharedMesh = panelMesh;
        PrefabUtility.RecordPrefabInstancePropertyModifications(source);
        var result = new GameObject("CompletionButton", typeof(MeshFilter), typeof(MeshRenderer));
        Undo.RegisterCreatedObjectUndo(result, "Separate completion button cap");
        result.transform.SetParent(source.transform.parent, false);
        result.transform.SetLocalPositionAndRotation(source.transform.localPosition, source.transform.localRotation);
        result.transform.localScale = source.transform.localScale;
        result.GetComponent<MeshFilter>().sharedMesh = capMesh;
        result.GetComponent<MeshRenderer>().sharedMaterial = source.GetComponent<Renderer>().sharedMaterial;
        AssetDatabase.SaveAssets();
        return result;
    }
}
