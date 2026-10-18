using System;
using System.Linq;
using GameJam2026.Levers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class LeversSetup
{
    [MenuItem("Tools/Minigames/Levers/Assemble")]
    public static void Assemble()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/ControllerScene.unity")
            throw new InvalidOperationException("Open ControllerScene in Edit Mode first.");
        if (UnityEngine.Object.FindFirstObjectByType<LeversBoard>() != null)
            throw new InvalidOperationException("Levers already assembled; edit the existing board.");
        var panel = GameObject.Find("ShipPanel").GetComponentsInChildren<Transform>()
            .Single(t => t.name == "Painel" && t.parent.name == "ShipPanel");
        var baseRenderer = panel.Find("Merged_pCube3_2").GetComponent<Renderer>();
        var light = panel.Find("pSphere4").GetComponent<Renderer>();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Panel/new/Alavanca.fbx");
        var boardObject = new GameObject("Levers");
        Undo.RegisterCreatedObjectUndo(boardObject, "Assemble Levers");
        boardObject.transform.SetParent(GameObject.Find("Minigames").transform, false);
        var board = boardObject.AddComponent<LeversBoard>();
        var handles = new LeverHandle[3];
        for (int i = 0; i < handles.Length; i++)
        {
            var handle = new GameObject("Lever_" + (i + 1));
            handle.transform.SetParent(boardObject.transform, false);
            handle.transform.SetPositionAndRotation(panel.TransformPoint(new Vector3(24.5f + i * 8.85f, 92f, 75f)), panel.rotation);
            var pivot = new GameObject("MovingPivot").transform;
            pivot.SetParent(handle.transform, false);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, pivot);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * .01f;
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = baseRenderer.sharedMaterial;
            // The full rail stays selectable at every handle position.
            var collider = handle.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .10f, 0);
            collider.size = new Vector3(.15f, .37f, .42f);
            handles[i] = handle.AddComponent<LeverHandle>();
            Set(handles[i], "movingPivot", pivot);
            handles[i].ShowPosition(1);
        }
        var camera = Camera.main;
        var controller = camera.GetComponent<CameraController>();
        var target = new GameObject("Levers").transform;
        target.SetParent(GameObject.Find("CameraPositions").transform, false);
        Vector3 center = panel.TransformPoint(new Vector3(49f, 95f, 72f));
        target.position = center + new Vector3(0, 1.9f, -2.8f);
        target.LookAt(center);
        AddPositionMarks(panel, handles);
        Set(board, "cameraController", controller); Set(board, "cameraTarget", target); Set(board, "statusLight", light);
        var serialized = new SerializedObject(board);
        var array = serialized.FindProperty("levers"); array.arraySize = handles.Length;
        for (int i = 0; i < handles.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = handles[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        const string materialPath = "Assets/Art/Materials/M_LeversStatus.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_LightSequenceSiren.mat"));
            material.name = "M_LeversStatus";
            AssetDatabase.CreateAsset(material, materialPath);
        }
        light.sharedMaterial = material;
        material.SetColor("_BaseColor", new Color(1f, .055f, .015f));
        material.SetColor("_EmissionColor", new Color(1f, .055f, .015f) * 2.4f);
        material.EnableKeyword("_EMISSION");
        var area = new GameObject("LeversClickArea", typeof(BoxCollider), typeof(CameraClickArea));
        area.transform.SetParent(GameObject.Find("ShipPanel").transform, false);
        area.transform.SetPositionAndRotation(panel.TransformPoint(new Vector3(33.35f, 99f, 75f)), panel.rotation);
        area.GetComponent<BoxCollider>().center = new Vector3(0, .15f, 0);
        area.GetComponent<BoxCollider>().size = new Vector3(.68f, .65f, .65f);
        Set(area.GetComponent<CameraClickArea>(), "cameraController", controller);
        Set(area.GetComponent<CameraClickArea>(), "targetPosition", target);
        if (camera.GetComponent<PhysicsRaycaster>() == null) camera.gameObject.AddComponent<PhysicsRaycaster>();
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
    }

    public static void AddPositionMarks(Transform panel, LeverHandle[] handles)
    {
        const string materialPath = "Assets/Art/Materials/M_LeversTicks.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "M_LeversTicks";
            material.SetColor("_BaseColor", new Color(.12f, .14f, .15f));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        foreach (var handle in handles)
            foreach (var child in handle.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Position_")).ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        var rail = panel.Find("Merged_pCube3_2");
        var collider = rail.gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = rail.GetComponent<MeshFilter>().sharedMesh;
        var preview = new GameObject("Levers mark alignment camera").AddComponent<Camera>();
        preview.enabled = false;
        preview.CopyFrom(Camera.main);
        preview.enabled = false;
        var view = GameObject.Find("CameraPositions/Levers").transform;
        preview.transform.SetPositionAndRotation(view.position, view.rotation);
        try
        {
            for (int i = 0; i < handles.Length; i++)
            {
                var handle = handles[i];
                int originalPosition = handle.Position;
                var filter = handle.GetComponentInChildren<MeshFilter>();
                Vector3 gripCenter = GripCenter(filter.sharedMesh);
                try
                {
                for (int level = 1; level <= 4; level++)
                {
                    handle.ShowPosition(level);
                    float targetHeight = preview.WorldToViewportPoint(filter.transform.TransformPoint(gripCenter)).y;
                    Vector3 normal = panel.TransformDirection(new Vector3(0, .94f, -.34f));
                    // Fit each tick to the projected grip center, including perspective and rail curvature.
                    Vector3 markPosition = AlignedMark(panel, collider, preview, 20.3f + i * 8.85f, targetHeight, normal);
                    var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mark.name = "Position_" + level;
                    UnityEngine.Object.DestroyImmediate(mark.GetComponent<Collider>());
                    mark.transform.SetParent(handles[i].transform, false);
                    mark.transform.SetPositionAndRotation(markPosition,
                        Quaternion.LookRotation(-normal, panel.TransformDirection(new Vector3(0, .34f, .94f))));
                    mark.transform.localScale = new Vector3(.026f, .004f, .001f);
                    mark.GetComponent<Renderer>().sharedMaterial = material;
                }
                }
                finally { handle.ShowPosition(originalPosition); }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(collider);
            UnityEngine.Object.DestroyImmediate(preview.gameObject);
        }
    }

    public static Vector3 GripCenter(Mesh mesh)
    {
        // Center of the visible end face of the supplied tapered handle, not its shaft.
        return new Vector3(mesh.bounds.center.x, mesh.bounds.max.y, mesh.bounds.center.z);
    }

    private static Vector3 AlignedMark(Transform panel, Collider rail, Camera camera, float x, float targetHeight, Vector3 normal)
    {
        float lower = 66f, upper = 81f;
        Vector3 best = Vector3.zero;
        float bestError = float.MaxValue;
        for (int iteration = 0; iteration < 24; iteration++)
        {
            float z = (lower + upper) * .5f;
            var ray = new Ray(panel.TransformPoint(new Vector3(x, 180, z)), panel.TransformDirection(Vector3.down));
            if (!rail.Raycast(ray, out var hit, 10))
                throw new InvalidOperationException("Lever tick falls outside the rail surface.");
            Vector3 position = hit.point + normal * .004f;
            float height = camera.WorldToViewportPoint(position).y;
            float error = Mathf.Abs(height - targetHeight);
            if (error < bestError) { bestError = error; best = position; }
            if (height < targetHeight) lower = z;
            else upper = z;
        }
        if (bestError > .001f)
            throw new InvalidOperationException("Lever grip cannot be aligned to a tick within the rail.");
        return best;
    }

    private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
