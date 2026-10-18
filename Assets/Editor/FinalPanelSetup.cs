using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GameJam2026.LightSequence;
using GameJam2026.Levers;
using GameJam2026.CelestialLock;

public static class FinalPanelSetup
{
    private const string Art = "Assets/Art/Panel/new2/";
    private const string Scene = "Assets/Scenes/ControllerScene.unity";

    [MenuItem("Tools/Ship/Use Final Panel")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != Scene) throw new InvalidOperationException("Open ControllerScene in Edit Mode.");
        var ship = GameObject.Find("ShipPanel").transform;
        if (ship.Find("Painel_Legacy") != null) throw new InvalidOperationException("Final panel already installed; adjust the existing objects.");
        var old = ship.Find("Painel");
        var oldSiren = old.GetComponentInChildren<LightSequenceStatusLight>();
        var levers = UnityEngine.Object.FindFirstObjectByType<LeversBoard>();
        var celestial = UnityEngine.Object.FindFirstObjectByType<CelestialLockBoard>();
        var oldLever = (Renderer)new SerializedObject(levers).FindProperty("statusLight").objectReferenceValue;
        var oldLock = (Renderer)new SerializedObject(celestial).FindProperty("statusLight").objectReferenceValue;
        var panel = Instantiate("Painel.fbx", ship, "Painel");
        panel.transform.localPosition = old.localPosition;
        panel.transform.localRotation = old.localRotation;
        panel.transform.localScale = old.localScale;
        panel.GetComponentInChildren<Renderer>().sharedMaterial = PanelMaterial();

        var siren = CreateLight(panel.transform, "Sirene.fbx", "Sirene_LightSequence",
            oldSiren.GetComponent<Renderer>(), .03f, old.rotation);
        var status = Undo.AddComponent<LightSequenceStatusLight>(siren.gameObject);
        EditorUtility.CopySerialized(oldSiren, status);
        Set(status, "lightRenderer", siren);
        Set(levers, "statusLight", CreateLight(panel.transform, "luz_alavanca.fbx", "Luz_Alavancas", oldLever, .025f, old.rotation));
        Set(celestial, "statusLight", CreateLight(panel.transform, "luz_alavanca.fbx", "Luz_Cadeado", oldLock, .025f, old.rotation));
        Undo.RecordObject(old.gameObject, "Retire old panel");
        old.name = "Painel_Legacy";
        old.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Final panel installed: Sirene_LightSequence, Luz_Alavancas and Luz_Cadeado wired to their original red/green behavior.");
    }

    private static GameObject Instantiate(string file, Transform parent, string name)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + file);
        if (model == null) throw new InvalidOperationException("Missing model: " + file);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        Undo.RegisterCreatedObjectUndo(go, "Install final panel");
        go.name = name;
        return go;
    }

    private static Renderer CreateLight(Transform panel, string model, string name, Renderer source, float scale, Quaternion rotation)
    {
        var go = Instantiate(model, panel, name);
        go.transform.rotation = rotation;
        // FBX models use centimeters. Keep each lamp independently editable in the scene.
        go.transform.localScale = Vector3.one * (scale / panel.lossyScale.x);
        var renderer = go.GetComponentInChildren<Renderer>();
        renderer.sharedMaterial = source.sharedMaterial;
        go.transform.position += source.bounds.center - renderer.bounds.center;
        return renderer;
    }

    private static Material PanelMaterial()
    {
        const string path = "Assets/Art/Materials/M_FinalPanel.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_FinalPanel" };
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Painel_Albedo.png"));
        material.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Painel_AO.png"));
        material.EnableKeyword("_OCCLUSIONMAP");
        var normal = (TextureImporter)AssetImporter.GetAtPath(Art + "Painel_Normal.png");
        if (normal.textureType != TextureImporterType.NormalMap) { normal.textureType = TextureImporterType.NormalMap; normal.SaveAndReimport(); }
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Painel_Normal.png"));
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Smoothness", .25f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        Undo.RecordObject(target, "Connect final panel light");
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }
}
