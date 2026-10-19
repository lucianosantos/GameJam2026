using System;
using System.IO;
using System.Linq;
using GameJam2026;
using GameJam2026.Levers;
using GameJam2026.LightSequence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit migration: importing this script never changes an open scene.
public static class PanelVisualsSetup
{
    private const string ScenePath = "Assets/Scenes/ControllerScene.unity";
    private const string ButtonPrefab = "Assets/Prefabs/LightSequenceButton.prefab";
    private const string LeverModel = "Assets/Art/Panel/new2/Alavanca.fbx";
    private const string ButtonModel = "Assets/Art/Panel/new2/Botão luz.fbx";
    private const string GlassMaterial = "Assets/Art/Materials/M_CockpitGlass.mat";

    [MenuItem("Tools/Ship/Apply Window and New Controls")]
    public static void Apply()
    {
        if (Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Open ControllerScene in Edit Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var symbols = Object.FindObjectsByType<LightSequenceButton>(FindObjectsSortMode.None)
            .ToDictionary(b => b.ButtonId, b => b.transform.Find("ZodiacSymbol").GetComponent<Image>().sprite);
        ReplaceButtonPrefab();
        ReplaceLevers();
        InstallWindow();
        // Refresh clearance references after replacing renderers and adding the windscreen.
        var data = new SerializedObject(Object.FindFirstObjectByType<GameSession>());
        var geometry = data.FindProperty("shipGeometry");
        var parts = GameObject.Find("ShipPanel").GetComponentsInChildren<Renderer>()
            .Concat(GameObject.Find("Minigames").GetComponentsInChildren<Renderer>())
            .Concat(GameObject.Find("StatusMonitor").GetComponentsInChildren<Renderer>())
            .Where(r => r.enabled).Distinct().ToArray();
        geometry.arraySize = parts.Length;
        for (int i = 0; i < parts.Length; i++) geometry.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        foreach (var button in Object.FindObjectsByType<LightSequenceButton>(FindObjectsSortMode.None))
            Check(button.transform.Find("ZodiacSymbol").GetComponent<Image>().sprite == symbols[button.ButtonId], "Zodiac symbol changed");
        Validate();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ReplaceButtonPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(ButtonPrefab);
        try
        {
            var data = new SerializedObject(root.GetComponent<LightSequenceButton>());
            var old = (Transform)data.FindProperty("visual").objectReferenceValue;
            if (old.GetComponentInChildren<MeshFilter>().sharedMesh == AssetDatabase.LoadAssetAtPath<GameObject>(ButtonModel).GetComponentInChildren<MeshFilter>().sharedMesh) return;
            var material = ((Renderer)data.FindProperty("lightRenderer").objectReferenceValue).sharedMaterial;
            Object.DestroyImmediate(old.gameObject);
            var visual = new GameObject("Btn").transform;
            visual.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonModel), visual);
            var bounds = model.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            var hit = root.GetComponent<BoxCollider>();
            // The new export faces +Y; the button's outward normal is local -Z.
            model.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            model.transform.localScale = new Vector3(hit.size.x / bounds.size.x, hit.size.z / bounds.size.y, hit.size.y / bounds.size.z);
            model.transform.localPosition = -(model.transform.localRotation * Vector3.Scale(bounds.center, model.transform.localScale));
            var renderer = model.GetComponentInChildren<Renderer>();
            renderer.sharedMaterial = material;
            Record(model.transform, renderer);
            data.FindProperty("visual").objectReferenceValue = visual;
            data.FindProperty("lightRenderer").objectReferenceValue = renderer;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ReplaceLevers()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(LeverModel);
        foreach (var handle in Object.FindObjectsByType<LeverHandle>(FindObjectsSortMode.None))
        {
            var pivot = (Transform)new SerializedObject(handle).FindProperty("movingPivot").objectReferenceValue;
            var old = pivot.GetComponentInChildren<MeshFilter>();
            if (old.sharedMesh == model.GetComponentInChildren<MeshFilter>().sharedMesh) continue;
            var material = old.GetComponent<Renderer>().sharedMaterial;
            var oldRoot = old.transform;
            while (oldRoot.parent != pivot) oldRoot = oldRoot.parent;
            Vector3 scale = oldRoot.localScale;
            // Preserve grip height and the existing four-position pivot animation.
            scale.y *= old.sharedMesh.bounds.max.y / model.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.max.y;
            Object.DestroyImmediate(oldRoot.gameObject);
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(model, pivot);
            replacement.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            replacement.transform.localScale = scale;
            foreach (var renderer in replacement.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
                Record(renderer);
            }
            Record(replacement.transform);
        }
    }

    private static void InstallWindow()
    {
        var parent = GameObject.Find("ShipPanel").transform;
        var window = parent.Find("CockpitWindow");
        if (window == null)
        {
            var existing = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .FirstOrDefault(f => AssetDatabase.GetAssetPath(f.sharedMesh) == "Assets/Art/Panel/janela.fbx");
            if (existing != null)
            {
                window = existing.transform;
                while (window.parent != null && window.parent != parent && PrefabUtility.IsPartOfPrefabInstance(window.parent)) window = window.parent;
                window.SetParent(parent, true);
                window.name = "CockpitWindow";
            }
        }
        if (window == null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Panel/janela.fbx"), parent);
            go.name = "CockpitWindow";
            window = go.transform;
        }
        window.SetLocalPositionAndRotation(new Vector3(0, .035f, 1.33f), Quaternion.identity);
        window.localScale = new Vector3(.0235f, .0105f, .0235f);
        var material = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterial);
        if (material == null)
        {
            material = new Material(Shader.Find("Ship/Cockpit Glass")) { name = "M_CockpitGlass" };
            AssetDatabase.CreateAsset(material, GlassMaterial);
        }
        var renderer = window.GetComponent<Renderer>();
        material.SetColor("_BaseColor", new Color(.09f, .16f, .18f, .018f));
        material.SetColor("_EdgeColor", new Color(.32f, .48f, .54f, .18f));
        EditorUtility.SetDirty(material);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        // No collider: the windscreen must never intercept puzzle clicks.
        Record(window, renderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(window.gameObject);
    }

    [MenuItem("Tools/Validation/Check Window and New Controls")]
    public static void Validate()
    {
        var buttons = Object.FindObjectsByType<LightSequenceButton>(FindObjectsSortMode.None);
        Check(buttons.Length == 8, "Expected eight light sequence buttons");
        foreach (var button in buttons)
        {
            var renderer = (Renderer)new SerializedObject(button).FindProperty("lightRenderer").objectReferenceValue;
            Check(renderer != null && AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh) == ButtonModel, "Incorrect light button model");
            var symbol = button.transform.Find("ZodiacSymbol");
            Check(symbol != null && symbol.GetComponent<Image>().sprite != null && !symbol.GetComponent<Image>().raycastTarget, "Missing zodiac symbol");
            Check(symbol.localPosition.z < -button.GetComponent<BoxCollider>().size.z * .5f, "Symbol clips into button");
        }
        var handles = Object.FindObjectsByType<LeverHandle>(FindObjectsSortMode.None);
        Check(handles.Length == 3, "Expected three levers");
        foreach (var handle in handles)
        {
            var pivot = (Transform)new SerializedObject(handle).FindProperty("movingPivot").objectReferenceValue;
            Check(pivot != null && AssetDatabase.GetAssetPath(pivot.GetComponentInChildren<MeshFilter>().sharedMesh) == LeverModel, "Incorrect lever model or missing pivot");
        }
        var window = GameObject.Find("ShipPanel/CockpitWindow");
        Check(window != null && window.GetComponent<Renderer>().sharedMaterial.shader.name == "Ship/Cockpit Glass", "Missing glass window");
        Check(window.GetComponentsInChildren<Collider>().Length == 0, "Glass must not block interaction");
        Check(!ShaderUtil.ShaderHasError(window.GetComponent<Renderer>().sharedMaterial.shader), "Glass shader has errors");
        Debug.Log("PASS: new2 models on 8 buttons and 3 animated levers; 8 zodiac symbols preserved; transparent window without input obstruction.");
    }

    public static void RunBatch()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Apply();
        Directory.CreateDirectory("Library/PanelVisuals");
        var camera = Camera.main;
        var originalPosition = camera.transform.position;
        var originalRotation = camera.transform.rotation;
        foreach (string view in new[] { "Overview", "LightSequence", "Levers" })
        {
            var target = GameObject.Find("CameraPositions/" + view).transform;
            camera.transform.SetPositionAndRotation(target.position, target.rotation);
            StatusMonitorSetup.Capture(camera, "Library/PanelVisuals/" + view + ".png");
        }
        camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
        Validate();
        MinigameInteractionChecks.Run();
    }

    private static void Record(params Object[] values)
    {
        foreach (var value in values)
            if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
