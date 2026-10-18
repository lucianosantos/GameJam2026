using System;
using System.Collections.Generic;
using System.IO;
using GameJam2026.Wires;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Authoring tools only. Runtime never creates materials, loads assets by path or rebuilds the panel.
public static class WiresSetup
{
    private const string Art = "Assets/Art/Minigames/Wires";
    private static readonly string[] Ids = { "fio_z", "fio_x", "fio_musica", "fio_infinito", "fio_e", "fio_m", "fio_p" };

    [MenuItem("Tools/Minigames/Wires/Create Assets and Assemble")]
    public static void Assemble()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before assembling.");
        var root = GameObject.Find("Minigames/Wires");
        if (root == null) throw new InvalidOperationException("Open ControllerScene with Minigames/Wires.");
        if (root.GetComponent<WiresBoard>() != null)
            throw new InvalidOperationException("Wires is already assembled. Edit its existing assets and Inspector fields.");
        var panel = root.transform.Find("GameCanvas") as RectTransform;
        var tray = root.transform.Find("WiresCanvas") as RectTransform;
        if (panel == null || tray == null) throw new InvalidOperationException("Expected GameCanvas and WiresCanvas.");
        var camera = Camera.main;
        var controller = camera != null ? camera.GetComponent<CameraController>() : null;
        var positions = GameObject.Find("CameraPositions");
        if (controller == null || positions == null) throw new InvalidOperationException("Missing CameraController or CameraPositions.");
        Directory.CreateDirectory(Art + "/Meshes"); Directory.CreateDirectory(Art + "/Materials");
        Directory.CreateDirectory(Art + "/Textures"); Directory.CreateDirectory(Art + "/Prefabs");
        AssetDatabase.Refresh();
        Mesh box = MeshAsset("Box", Box());
        Mesh ring = MeshAsset("SocketRing", Lathe(new[] { new Vector2(.026f, 0), new Vector2(.026f, -.015f), new Vector2(.014f, -.015f), new Vector2(.014f, 0) }));
        Mesh disk = MeshAsset("SocketWell", Lathe(new[] { new Vector2(.014f, .003f), new Vector2(.014f, 0), new Vector2(0, 0), new Vector2(0, .003f) }));
        Mesh sleeve = MeshAsset("PlugSleeve", Lathe(new[] { new Vector2(.0115f, .010f), new Vector2(.0115f, -.008f), new Vector2(0, -.008f), new Vector2(0, .010f) }));
        Mesh tip = MeshAsset("PlugTip", Lathe(new[] { new Vector2(.009f, .024f), new Vector2(.009f, 0), new Vector2(0, 0), new Vector2(0, .024f) }));
        Mesh labelMesh = MeshAsset("SymbolQuad", Quad());
        var steel = MaterialAsset("PanelSteel", new Color(.30f, .33f, .35f), .65f, .32f, MetalTexture());
        var rim = MaterialAsset("RimSteel", new Color(.15f, .18f, .20f), .7f, .4f);
        var socketMetal = MaterialAsset("SocketMetal", new Color(.57f, .61f, .63f), .8f, .5f);
        var rubber = MaterialAsset("RubberOrange", new Color(.88f, .23f, .035f), 0f, .28f);
        var black = MaterialAsset("SocketInterior", new Color(.018f, .022f, .025f), .1f, .15f);
        var brass = MaterialAsset("PlugContact", new Color(.7f, .48f, .19f), .8f, .5f);
        var symbols = new Material[Ids.Length];
        for (int i = 0; i < symbols.Length; i++) symbols[i] = MaterialAsset("Symbol_" + Ids[i], Color.white, 0, .15f, SymbolTexture(i));

        GameObject single = Prefab("Connector", () =>
        {
            var go = new GameObject("Connector");
            Part("Mount", go.transform, box, socketMetal, new Vector3(0, 0, .001f), new Vector3(.067f, .067f, .006f));
            Part("Ring", go.transform, ring, socketMetal, new Vector3(0, 0, -.006f), Vector3.one);
            Part("Dark well", go.transform, disk, black, new Vector3(0, 0, -.008f), Vector3.one);
            var socket = go.AddComponent<WiresSocket>();
            var anchor = Child("PlugAnchor", go.transform); anchor.localPosition = new Vector3(0, 0, -.038f);
            Set(socket, "anchor", anchor); Set(socket, "portId", "unassigned");
            return go;
        });
        GameObject doubleConnector = Prefab("DoubleConnectorZ", () =>
        {
            var go = new GameObject("DoubleConnectorZ");
            Part("Shared mount", go.transform, box, rim, Vector3.zero, new Vector3(.15f, .079f, .008f));
            foreach (float x in new[] { -.038f, .038f })
            {
                var socket = (GameObject)PrefabUtility.InstantiatePrefab(single);
                socket.transform.SetParent(go.transform, false); socket.transform.localPosition = new Vector3(x, 0, -.004f);
                Set(socket.GetComponent<WiresSocket>(), "portId", "fio_z");
            }
            return go;
        });
        GameObject plug = Prefab("Plug", () =>
        {
            var go = new GameObject("Plug");
            Part("Contact", go.transform, tip, brass, Vector3.zero, Vector3.one);
            Part("Sleeve", go.transform, sleeve, rubber, Vector3.zero, Vector3.one);
            var collider = go.AddComponent<SphereCollider>(); collider.radius = .018f;
            return go;
        });
        GameObject cablePrefab = Prefab("Cable", () =>
        {
            var go = new GameObject("Cable");
            var a = (GameObject)PrefabUtility.InstantiatePrefab(plug); a.name = "End A"; a.transform.SetParent(go.transform, false);
            var b = (GameObject)PrefabUtility.InstantiatePrefab(plug); b.name = "End B"; b.transform.SetParent(go.transform, false);
            a.transform.localPosition = new Vector3(0, -.10f, -.038f); b.transform.localPosition = new Vector3(0, .10f, -.038f);
            var tubeObject = new GameObject("Cable tube", typeof(MeshFilter), typeof(MeshRenderer));
            tubeObject.transform.SetParent(go.transform, false); tubeObject.GetComponent<MeshRenderer>().sharedMaterial = rubber;
            var cable = go.AddComponent<WiresCable>();
            Set(cable, "firstEnd", a.transform); Set(cable, "secondEnd", b.transform);
            Set(cable, "firstGrabCollider", a.GetComponent<Collider>()); Set(cable, "secondGrabCollider", b.GetComponent<Collider>());
            Set(cable, "tube", tubeObject.GetComponent<MeshFilter>());
            return go;
        });
        GameObject panelPrefab = Prefab("Panel", () => BoxWithRim("Panel", box, steel, rim, new Vector2(.52f, .8f)));
        GameObject trayPrefab = Prefab("WireTray", () => BoxWithRim("WireTray", box, steel, rim, new Vector2(.33f, .33f)));
        var baseObject = Instance(panelPrefab, panel, "Panel");
        baseObject.localScale = new Vector3(panel.rect.width / .52f, panel.rect.height / .8f, 1);
        var trayObject = Instance(trayPrefab, tray, "WireTray");
        trayObject.localScale = new Vector3(tray.rect.width / .33f, tray.rect.height / .33f, 1);

        float halfW = panel.rect.width * .5f, halfH = panel.rect.height * .5f;
        var z = Instance(doubleConnector, panel, "fio_z"); z.localPosition = new Vector3(0, halfH - .085f, 0);
        var allSockets = new List<WiresSocket>(z.GetComponentsInChildren<WiresSocket>());
        Label(panel, "fio_z", new Vector2(0, halfH - .033f), symbols[0], labelMesh);
        int[] symbolIndices = { 1, 3, 5, 2, 4, 6 };
        for (int i = 0; i < 6; i++)
        {
            bool left = i < 3; int row = i % 3; int index = symbolIndices[i];
            float x = (halfW - .095f) * (left ? -1 : 1), y = (1 - row) * (panel.rect.height * .28f);
            var socket = Instance(single, panel, Ids[index]); socket.localPosition = new Vector3(x, y, 0);
            Set(socket.GetComponent<WiresSocket>(), "portId", Ids[index]); allSockets.Add(socket.GetComponent<WiresSocket>());
            Label(panel, Ids[index], new Vector2((halfW - .038f) * (left ? -1 : 1), y), symbols[index], labelMesh);
        }
        var cables = new WiresCable[4];
        for (int i = 0; i < cables.Length; i++)
        {
            var cable = Instance(cablePrefab, tray, "Cable " + (i + 1));
            cable.localPosition = new Vector3((i - 1.5f) * .065f, 0, 0);
            cables[i] = cable.GetComponent<WiresCable>();
        }
        string rulesPath = Art + "/WiresRules.asset";
        var rules = AssetDatabase.LoadAssetAtPath<WiresRules>(rulesPath);
        if (rules == null)
        {
            rules = ScriptableObject.CreateInstance<WiresRules>();
            var serialized = new SerializedObject(rules); var pairs = serialized.FindProperty("solution"); pairs.arraySize = 4;
            string[] a = { "fio_p", "fio_musica", "fio_z", "fio_x" };
            string[] b = { "fio_z", "fio_m", "fio_infinito", "fio_e" };
            for (int i = 0; i < 4; i++)
            { pairs.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue = a[i]; pairs.GetArrayElementAtIndex(i).FindPropertyRelative("second").stringValue = b[i]; }
            serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(rules, rulesPath);
        }
        var target = Child("Wires", positions.transform);
        // Fit both existing canvases without changing the camera FOV or their authored transforms.
        Vector3 trayCenter = panel.InverseTransformPoint(tray.position);
        float minX = Mathf.Min(-halfW, trayCenter.x - tray.rect.width / 2), maxX = halfW;
        float minY = Mathf.Min(-halfH, trayCenter.y - tray.rect.height / 2), maxY = Mathf.Max(halfH, trayCenter.y + tray.rect.height / 2);
        float vertical = (maxY - minY) / 2 * 1.18f;
        float horizontal = (maxX - minX) / 2 / (16f / 9f) * 1.18f;
        float distance = Mathf.Max(vertical, horizontal) / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2);
        Vector3 focus = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, 0);
        Quaternion tilt = Quaternion.Euler(-22, 0, 0);
        target.SetPositionAndRotation(panel.TransformPoint(focus + tilt * Vector3.back * distance), panel.rotation * tilt);
        var board = root.AddComponent<WiresBoard>();
        Set(board, "surface", panel); Set(board, "cameraController", controller); Set(board, "cameraTarget", target);
        Set(board, "interactionCamera", camera); Set(board, "rules", rules); Set(board, "looseTipDepth", -.038f);
        SetArray(board, "sockets", allSockets.ToArray()); SetArray(board, "cables", cables);
        foreach (var canvas in new[] { panel, tray })
        {
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false; // 3D colliders handle all plugs.
            var area = Child("WiresClickArea", canvas);
            var collider = area.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(canvas.rect.width, canvas.rect.height, .01f); collider.center = new Vector3(0, 0, -.065f);
            var click = area.gameObject.AddComponent<CameraClickArea>();
            Set(click, "cameraController", controller); Set(click, "targetPosition", target);
            area.SetParent(GameObject.Find("ShipPanel").transform, true);
        }
        board.RefreshCableGeometry();
        EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        Debug.Log("Wires assembled: editable meshes, PNGs, materials, prefabs, rules and scene references. Save the scene after inspection.");
    }

    public static void AssembleBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ControllerScene.unity");
        Assemble();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static Transform Child(string name, Transform parent)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
    private static Transform Instance(GameObject prefab, Transform parent, string name)
    { var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); go.name = name; return go.transform; }

    private static void Label(Transform parent, string name, Vector2 position, Material material, Mesh quad)
    { Part("Symbol_" + name, parent, quad, material, new Vector3(position.x, position.y, -.008f), new Vector3(.042f, .042f, 1)); }

    private static GameObject BoxWithRim(string name, Mesh mesh, Material baseMat, Material rimMat, Vector2 size)
    {
        var go = new GameObject(name);
        Part("Base", go.transform, mesh, baseMat, new Vector3(0, 0, .012f), new Vector3(size.x, size.y, .016f));
        foreach (float sign in new[] { -1f, 1f })
        {
            Part("Side", go.transform, mesh, rimMat, new Vector3(sign * (size.x / 2 - .009f), 0, .003f), new Vector3(.018f, size.y, .032f));
            Part("Edge", go.transform, mesh, rimMat, new Vector3(0, sign * (size.y / 2 - .009f), .003f), new Vector3(size.x, .018f, .032f));
        }
        return go;
    }

    private static GameObject Part(string name, Transform parent, Mesh mesh, Material material, Vector3 position, Vector3 scale)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }
    private static GameObject Prefab(string name, Func<GameObject> build)
    {
        string path = Art + "/Prefabs/" + name + ".prefab";
        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (saved != null) return saved;
        var temporary = build(); saved = PrefabUtility.SaveAsPrefabAsset(temporary, path); UnityEngine.Object.DestroyImmediate(temporary); return saved;
    }
    private static Mesh MeshAsset(string name, Mesh mesh)
    {
        string path = Art + "/Meshes/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) { UnityEngine.Object.DestroyImmediate(mesh); return existing; }
        mesh.name = name; AssetDatabase.CreateAsset(mesh, path); return mesh;
    }
    private static Material MaterialAsset(string name, Color color, float metallic, float smoothness, Texture texture = null)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", smoothness);
        if (texture != null) mat.SetTexture("_BaseMap", texture);
        AssetDatabase.CreateAsset(mat, path); return mat;
    }
    private static void Set(UnityEngine.Object target, string field, object value)
    {
        var so = new SerializedObject(target); var p = so.FindProperty(field);
        if (value is UnityEngine.Object obj) p.objectReferenceValue = obj;
        else if (value is string text) p.stringValue = text;
        else if (value is float number) p.floatValue = number;
        else throw new ArgumentException("Unsupported serialized field " + field);
        so.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
    private static void SetArray<T>(UnityEngine.Object target, string field, T[] objects) where T : UnityEngine.Object
    {
        var so = new SerializedObject(target); var p = so.FindProperty(field); p.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Mesh Box()
    {
        var vertices = new List<Vector3>(); var indices = new List<int>(); var uv = new List<Vector2>();
        Vector3[] normals = { Vector3.back, Vector3.forward, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
        foreach (var normal in normals)
        {
            Vector3 right = Vector3.Cross(normal, Vector3.up);
            if (right.sqrMagnitude < .1f) right = Vector3.right;
            right.Normalize(); Vector3 up = Vector3.Cross(right, normal);
            int start = vertices.Count;
            vertices.Add((normal - right - up) / 2); vertices.Add((normal - right + up) / 2);
            vertices.Add((normal + right + up) / 2); vertices.Add((normal + right - up) / 2);
            uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
            indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); return mesh;
    }
    private static Mesh Quad()
    {
        var mesh = new Mesh();
        mesh.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0), new Vector3(.5f, -.5f, 0) };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateNormals(); return mesh;
    }
    private static Mesh Lathe(Vector2[] profile)
    {
        const int segments = 48;
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
        // Duplicate profile corners for clean hard edges on plugs and sockets.
        for (int p = 0; p < profile.Length; p++)
        {
            Vector2 a = profile[p], b = profile[(p + 1) % profile.Length]; int start = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                foreach (Vector2 v in new[] { a, b })
                { vertices.Add(new Vector3(Mathf.Cos(angle) * v.x, Mathf.Sin(angle) * v.x, v.y)); uv.Add(new Vector2(i / (float)segments, v == a ? 0 : 1)); }
            }
            for (int i = 0; i < segments; i++)
            { int n = start + i * 2; indices.AddRange(new[] { n, n + 1, n + 2, n + 2, n + 1, n + 3 }); }
        }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); return mesh;
    }
    private static Texture2D MetalTexture()
    {
        string path = Art + "/Textures/BrushedMetal.png";
        if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var texture = new Texture2D(256, 256, TextureFormat.RGB24, false); var random = new System.Random(71);
        for (int y = 0; y < 256; y++)
        {
            float row = (float)random.NextDouble() * .035f;
            for (int x = 0; x < 256; x++)
            { float value = .84f + row + (float)random.NextDouble() * .025f; texture.SetPixel(x, y, new Color(value, value, value)); }
        }
        return SaveTexture(texture, path);
    }
    private static Texture2D SymbolTexture(int symbol)
    {
        string path = Art + "/Textures/" + Ids[symbol] + ".png";
        if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var strokes = new List<Vector2[]>();
        switch (symbol)
        {
            case 0: strokes.Add(Stroke(.24f,.79f, .79f,.83f, .28f,.22f, .80f,.26f)); strokes.Add(Stroke(.23f,.48f, .70f,.51f)); break;
            case 1: strokes.Add(Stroke(.24f,.86f, .70f,.17f)); strokes.Add(Stroke(.71f,.85f, .30f,.20f)); break;
            case 2: strokes.Add(Stroke(.25f,.53f, .64f,.85f, .87f,.48f, .62f,.20f, .44f,.48f, .25f,.53f, .18f,.12f, .39f,.31f)); strokes.Add(Stroke(.76f,.73f, .86f,.32f)); break;
            case 3:
                var infinity = new Vector2[97];
                for (int i = 0; i < infinity.Length; i++) { float t = i * Mathf.PI * 2 / 96; infinity[i] = new Vector2(.5f + .36f * Mathf.Cos(t), .5f + .22f * Mathf.Sin(2*t)); }
                strokes.Add(infinity); break;
            case 4: strokes.Add(Stroke(.76f,.78f, .58f,.85f, .29f,.76f, .23f,.59f, .60f,.51f, .30f,.45f, .22f,.27f, .38f,.16f, .67f,.17f, .79f,.25f)); break;
            case 5: strokes.Add(Stroke(.23f,.19f, .20f,.82f, .49f,.46f, .72f,.85f, .79f,.19f)); break;
            case 6: strokes.Add(Stroke(.30f,.16f, .25f,.83f, .78f,.79f, .35f,.47f)); break;
        }
        var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
        {
            var point = new Vector2(x / 255f, y / 255f); float distance = float.PositiveInfinity;
            foreach (var stroke in strokes) for (int i = 0; i < stroke.Length - 1; i++)
            { Vector2 delta = stroke[i+1] - stroke[i]; float t = Mathf.Clamp01(Vector2.Dot(point - stroke[i], delta) / delta.sqrMagnitude); distance = Mathf.Min(distance, Vector2.Distance(point, stroke[i] + delta*t)); }
            texture.SetPixel(x, y, Color.Lerp(new Color(.045f,.055f,.06f), new Color(.84f,.82f,.69f), Mathf.SmoothStep(0, 1, (distance - .022f) / .006f)));
        }
        return SaveTexture(texture, path);
    }
    private static Vector2[] Stroke(params float[] coordinates)
    { var result = new Vector2[coordinates.Length / 2]; for (int i = 0; i < result.Length; i++) result[i] = new Vector2(coordinates[i*2], coordinates[i*2+1]); return result; }
    private static Texture2D SaveTexture(Texture2D texture, string path)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Repeat;
        importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
