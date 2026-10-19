using System;
using System.Collections.Generic;
using System.Linq;
using GameJam2026.Wires;
using GameJam2026.SymbolPassword;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PanelStatusLightsSetup
{
    private const string Incoming = "Assets/Art/Panel/new2/Painel (4).fbx";
    private const string PanelMeshPath = "Assets/Art/CompletionButton/Panel4WithoutCompletionCap.asset";

    [MenuItem("Tools/Ship/Update Panel Status Lights")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/ControllerScene.unity")
            throw new InvalidOperationException("Open ControllerScene in Edit Mode.");
        var panel = GameObject.Find("ShipPanel/Painel").transform;
        var filter = panel.Find("Painel_LP").GetComponent<MeshFilter>();
        var incoming = AssetDatabase.LoadAssetAtPath<GameObject>(Incoming).GetComponentInChildren<MeshFilter>().sharedMesh;
        if ((incoming.bounds.size - filter.sharedMesh.bounds.size).sqrMagnitude > .0001f)
            throw new InvalidOperationException("Unexpected panel dimensions; inspect the incoming model.");

        // This export centers the vertices around zero. Restore the existing mesh origin
        // so all puzzle surfaces, the monitor and the final button retain their alignment.
        Vector3 offset = filter.sharedMesh.bounds.center - incoming.bounds.center;
        // Build native mesh buffers explicitly. CopySerialized on an imported mesh
        // retained the previous rendered geometry even though CPU vertices changed.
        var updated = new Mesh { name = "Panel4WithoutCompletionCap", indexFormat = incoming.indexFormat };
        updated.vertices = incoming.vertices.Select(v => v + offset).ToArray();
        updated.normals = incoming.normals;
        updated.tangents = incoming.tangents;
        updated.uv = incoming.uv;
        updated.triangles = incoming.triangles;
        updated.RecalculateBounds();
        var sockets = SocketBounds(updated);
        if (sockets.Length != 4) throw new InvalidOperationException("Expected four disconnected 80-vertex lamp sockets.");
        var leverSocket = sockets.OrderByDescending(b => b.size.x).First();
        var lockSocket = sockets.OrderByDescending(b => b.center.x).First();
        var small = sockets.Where(b => b.size.x < lockSocket.size.x * .8f).OrderByDescending(b => b.center.y).ToArray();
        if (small.Length != 2) throw new InvalidOperationException("Expected two small sockets for wires and password.");

        // Keep the final cap independent and clickable; remove only its surface from
        // the new panel, after checking the cap's geometry and UV coordinates.
        var capFilter = panel.Find("CompletionButton").GetComponent<MeshFilter>();
        var cap = capFilter.sharedMesh;
        var vertices = updated.vertices;
        var uv = updated.uv;
        var capVertices = cap.vertices;
        var capUV = cap.uv;
        var indices = new HashSet<int>();
        for (int i = 0; i < capVertices.Length; i++)
        {
            int nearest = Enumerable.Range(0, vertices.Length).OrderBy(j =>
                (vertices[j] - capVertices[i]).sqrMagnitude + (uv[j] - capUV[i]).sqrMagnitude).First();
            if ((vertices[nearest] - capVertices[i]).sqrMagnitude > .0001f || (uv[nearest] - capUV[i]).sqrMagnitude > .0001f)
                throw new InvalidOperationException("Completion cap changed; original assets preserved.");
            indices.Add(nearest);
        }
        var triangles = updated.triangles;
        var remaining = new List<int>();
        int removed = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (indices.Contains(triangles[i]) && indices.Contains(triangles[i + 1]) && indices.Contains(triangles[i + 2])) removed += 3;
            else { remaining.Add(triangles[i]); remaining.Add(triangles[i + 1]); remaining.Add(triangles[i + 2]); }
        }
        if (removed != cap.triangles.Length) throw new InvalidOperationException("Unexpected completion cap topology.");
        updated.triangles = remaining.ToArray();
        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(PanelMeshPath);
        if (savedMesh == null)
        {
            AssetDatabase.CreateAsset(updated, PanelMeshPath);
            savedMesh = updated;
        }
        else
        {
            savedMesh.Clear();
            savedMesh.indexFormat = updated.indexFormat;
            savedMesh.vertices = updated.vertices;
            savedMesh.normals = updated.normals;
            savedMesh.tangents = updated.tangents;
            savedMesh.uv = updated.uv;
            savedMesh.triangles = updated.triangles;
            savedMesh.RecalculateBounds();
            Object.DestroyImmediate(updated);
        }
        EditorUtility.SetDirty(savedMesh);
        Undo.RecordObject(filter, "Use rebuilt panel geometry");
        filter.sharedMesh = savedMesh;
        PrefabUtility.RecordPrefabInstancePropertyModifications(filter);

        var lever = panel.Find("Luz_Alavancas");
        var lockLight = panel.Find("Luz_Cadeado");
        Fit(lockLight, filter.transform.TransformPoint(lockSocket.center), lever.localScale.x * lockSocket.size.x / leverSocket.size.x);
        AddLight(panel, filter.transform, lever, "Luz_Fios", small[0], leverSocket, "wiresBoard", Object.FindFirstObjectByType<WiresBoard>());
        AddLight(panel, filter.transform, lever, "Luz_Senha", small[1], leverSocket, "passwordBoard", Object.FindFirstObjectByType<SymbolPasswordBoard>());
        InstallIncomingModel(panel, filter);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Panel updated: one lever lamp, relocated lock lamp, and independent wires/password lamps fitted to the new sockets.");
    }

    private static void InstallIncomingModel(Transform panel, MeshFilter oldFilter)
    {
        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(oldFilter.gameObject) == Incoming) return;
        // Retain the scene container and the referenced puzzle objects, while replacing
        // the old FBX instance itself with the artist's new model.
        if (PrefabUtility.IsPartOfPrefabInstance(panel.gameObject))
            PrefabUtility.UnpackPrefabInstance(panel.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Incoming);
        var replacement = (GameObject)PrefabUtility.InstantiatePrefab(model, panel);
        Undo.RegisterCreatedObjectUndo(replacement, "Replace panel FBX");
        replacement.name = "Painel_LP";
        replacement.transform.SetLocalPositionAndRotation(oldFilter.transform.localPosition, oldFilter.transform.localRotation);
        replacement.transform.localScale = oldFilter.transform.localScale;
        // The sole mesh override separates the clickable final cap and compensates
        // the export origin; all four socket geometries come from Incoming.
        replacement.GetComponent<MeshFilter>().sharedMesh = oldFilter.sharedMesh;
        replacement.GetComponent<Renderer>().sharedMaterials = oldFilter.GetComponent<Renderer>().sharedMaterials;
        PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);
        PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.GetComponent<MeshFilter>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.GetComponent<Renderer>());
        Undo.DestroyObjectImmediate(oldFilter.gameObject);
    }

    private static void AddLight(Transform panel, Transform mesh, Transform template, string name, Bounds socket, Bounds large, string field, Object board)
    {
        var root = panel.Find(name);
        if (root == null)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Panel/new2/luz_alavanca.fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, panel);
            Undo.RegisterCreatedObjectUndo(go, "Add puzzle status light");
            go.name = name;
            root = go.transform;
        }
        root.rotation = template.rotation;
        Fit(root, mesh.TransformPoint(socket.center), template.localScale.x * socket.size.x / large.size.x);
        var renderer = root.GetComponentInChildren<Renderer>();
        string materialPath = "Assets/Art/Materials/M_" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(template.GetComponentInChildren<Renderer>().sharedMaterial) { name = "M_" + name };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        renderer.sharedMaterial = material;
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        var status = renderer.GetComponent<PuzzleCompletionLight>();
        if (status == null) status = Undo.AddComponent<PuzzleCompletionLight>(renderer.gameObject);
        var serialized = new SerializedObject(status);
        serialized.FindProperty(field).objectReferenceValue = board;
        serialized.ApplyModifiedProperties();
    }

    private static void Fit(Transform root, Vector3 center, float scale)
    {
        Undo.RecordObject(root, "Fit status lamp to socket");
        root.localScale = Vector3.one * scale;
        root.position += center - root.GetComponentInChildren<Renderer>().bounds.center;
        PrefabUtility.RecordPrefabInstancePropertyModifications(root);
    }

    private static Bounds[] SocketBounds(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var parent = Enumerable.Range(0, vertices.Length).ToArray();
        Func<int, int> root = null;
        root = i => parent[i] == i ? i : parent[i] = root(parent[i]);
        var points = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < vertices.Length; i++)
        {
            var key = Vector3Int.RoundToInt(vertices[i] * 1000);
            if (points.TryGetValue(key, out int other)) parent[root(i)] = root(other);
            else points[key] = i;
        }
        var triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        { parent[root(triangles[i + 1])] = root(triangles[i]); parent[root(triangles[i + 2])] = root(triangles[i]); }
        return Enumerable.Range(0, vertices.Length).GroupBy(root).Where(g => g.Count() == 80).Select(g =>
        {
            var bounds = new Bounds(vertices[g.First()], Vector3.zero);
            foreach (int i in g) bounds.Encapsulate(vertices[i]);
            return bounds;
        }).ToArray();
    }
}
