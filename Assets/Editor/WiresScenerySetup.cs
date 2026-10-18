using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GameJam2026.Wires;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WiresScenerySetup
{
    private const string Art = "Assets/Art/Minigames/Wires/";

    [MenuItem("Tools/Minigames/Wires/Add Fixed Scenery")]
    public static void Assemble()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var root = GameObject.Find("Minigames/Wires");
        var board = root != null ? root.GetComponent<WiresBoard>() : null;
        var surface = root != null ? root.transform.Find("GameCanvas") as RectTransform : null;
        if (board == null || surface == null) throw new InvalidOperationException("Open the configured wires scene.");
        if (surface.Find("Scenery") != null) throw new InvalidOperationException("Scenery already exists; edit the existing prefab.");
        EditorSceneManager.SaveScene(root.scene, "Temp/ControllerScene-before-wires-scenery.unity", true);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Prefabs/WiresScenery.prefab");
        if (prefab == null) prefab = CreatePrefab(surface.rect.size);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, surface);
        instance.name = "Scenery";
        var serialized = new SerializedObject(board);
        var tolerance = serialized.FindProperty("snapTolerance");
        float before = tolerance.floatValue; tolerance.floatValue = before * 1.8f;
        var obstacles = instance.GetComponentsInChildren<WiresCableObstacle>();
        var array = serialized.FindProperty("sceneryCables"); array.arraySize = obstacles.Length;
        for (int i = 0; i < obstacles.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = obstacles[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        board.InvalidateGeometry(); board.RefreshCableGeometry();
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(root.scene); EditorSceneManager.SaveScene(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("Wires: snap tolerance " + before + " -> " + tolerance.floatValue + "; four decorative ports, four symbols and two fixed cables added.");
    }

    private static GameObject CreatePrefab(Vector2 size)
    {
        var container = new GameObject("WiresScenery");
        try
        {
            Vector2[] normalized = { new Vector2(-.16f,.16f), new Vector2(.145f,.12f), new Vector2(-.135f,-.40f), new Vector2(.20f,-.375f) };
            float[] angles = { -12, 19, -17, 12 };
            string[] ids = { "espiral", "tridente", "losango_cortado", "escada" };
            var anchors = new Transform[4];
            var connectorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Prefabs/Connector.prefab");
            var quad = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "Meshes/SymbolQuad.asset");
            for (int i = 0; i < 4; i++)
            {
                var connector = (GameObject)PrefabUtility.InstantiatePrefab(connectorPrefab, container.transform);
                PrefabUtility.UnpackPrefabInstance(connector, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                connector.name = "SceneryPort_" + ids[i];
                var position = Vector2.Scale(normalized[i], size);
                connector.transform.localPosition = position; connector.transform.localRotation = Quaternion.Euler(0,0,angles[i]);
                anchors[i] = connector.transform.Find("PlugAnchor");
                foreach (var socket in connector.GetComponentsInChildren<WiresSocket>()) UnityEngine.Object.DestroyImmediate(socket);
                foreach (var collider in connector.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                var label = new GameObject("Symbol_" + ids[i], typeof(MeshFilter), typeof(MeshRenderer));
                label.transform.SetParent(container.transform, false);
                label.transform.localPosition = new Vector3(position.x, position.y + (i < 2 ? .053f : -.043f), -.008f);
                label.transform.localRotation = Quaternion.Euler(0,0,angles[i]); label.transform.localScale = new Vector3(.038f,.038f,1);
                label.GetComponent<MeshFilter>().sharedMesh = quad;
                label.GetComponent<MeshRenderer>().sharedMaterial = Symbol(i, ids[i]);
            }
            for (int pair = 0; pair < 2; pair++) BakeCable(container.transform, anchors[pair*2], anchors[pair*2+1], pair);
            return PrefabUtility.SaveAsPrefabAsset(container, Art + "Prefabs/WiresScenery.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(container); }
    }

    private static void BakeCable(Transform parent, Transform a, Transform b, int index)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Prefabs/Cable.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = "FixedCable_" + (index+1);
        var cable = go.GetComponent<WiresCable>();
        cable.End(0).position = a.position; cable.End(1).position = b.position;
        cable.Sample(parent); cable.DrawTube(parent);
        var path = (Vector3[])typeof(WiresCable).GetField("points", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cable);
        var filter = go.transform.Find("Cable tube").GetComponent<MeshFilter>();
        var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh); mesh.name = go.name; mesh.hideFlags = HideFlags.None;
        AssetDatabase.CreateAsset(mesh, Art + "Meshes/" + go.name + ".asset"); filter.sharedMesh = mesh;
        float radius = cable.Radius;
        UnityEngine.Object.DestroyImmediate(cable); // Baked scenery never runs cable input or deformation code.
        foreach (var collider in go.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        var obstacle = go.AddComponent<WiresCableObstacle>();
        var so = new SerializedObject(obstacle); so.FindProperty("radius").floatValue = radius;
        var line = so.FindProperty("centerline"); line.arraySize = path.Length;
        for (int i = 0; i < path.Length; i++) line.GetArrayElementAtIndex(i).vector3Value = go.transform.InverseTransformPoint(parent.TransformPoint(path[i]));
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material Symbol(int symbol, string id)
    {
        string path = Art + "Textures/scenery_" + id + ".png";
        var strokes = new List<Vector2[]>();
        switch(symbol)
        {
            case 0:
                var spiral = new Vector2[100];
                for(int i=0;i<spiral.Length;i++) { float t=i/99f, r=.34f*(1-.8f*t), angle=t*Mathf.PI*3.5f; spiral[i]=new Vector2(.5f+Mathf.Cos(angle)*r,.5f+Mathf.Sin(angle)*r); }
                strokes.Add(spiral); break;
            case 1: strokes.Add(Stroke(.48f,.15f,.51f,.83f)); strokes.Add(Stroke(.23f,.75f,.26f,.48f,.50f,.39f,.76f,.53f,.78f,.79f)); break;
            case 2: strokes.Add(Stroke(.51f,.85f,.82f,.52f,.50f,.17f,.19f,.49f,.51f,.85f)); strokes.Add(Stroke(.18f,.30f,.80f,.73f)); break;
            case 3: strokes.Add(Stroke(.26f,.16f,.23f,.82f)); strokes.Add(Stroke(.72f,.20f,.77f,.85f)); strokes.Add(Stroke(.25f,.34f,.73f,.37f)); strokes.Add(Stroke(.24f,.63f,.75f,.67f)); break;
        }
        if(!File.Exists(path))
        {
            var texture=new Texture2D(256,256,TextureFormat.RGB24,false);
            for(int y=0;y<256;y++) for(int x=0;x<256;x++)
            {
                var point=new Vector2(x/255f,y/255f); float nearest=float.PositiveInfinity;
                foreach(var stroke in strokes) for(int i=0;i<stroke.Length-1;i++)
                { Vector2 d=stroke[i+1]-stroke[i]; float t=Mathf.Clamp01(Vector2.Dot(point-stroke[i],d)/d.sqrMagnitude); nearest=Mathf.Min(nearest,Vector2.Distance(point,stroke[i]+d*t)); }
                texture.SetPixel(x,y,Color.Lerp(new Color(.045f,.055f,.06f),new Color(.84f,.82f,.69f),Mathf.SmoothStep(0,1,(nearest-.022f)/.006f)));
            }
            File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        string materialPath=Art+"Materials/ScenerySymbol_"+id+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null)
        {
            material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Art+"Materials/Symbol_fio_x.mat"));
            material.name="ScenerySymbol_"+id; material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            AssetDatabase.CreateAsset(material,materialPath);
        }
        return material;
    }
    private static Vector2[] Stroke(params float[] coordinates)
    { var points=new Vector2[coordinates.Length/2]; for(int i=0;i<points.Length;i++) points[i]=new Vector2(coordinates[i*2],coordinates[i*2+1]); return points; }
}
