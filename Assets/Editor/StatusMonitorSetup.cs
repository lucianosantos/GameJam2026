using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using GameJam2026;
using GameJam2026.LightSequence;
using GameJam2026.Wires;
using GameJam2026.SymbolPassword;
using GameJam2026.Levers;
using GameJam2026.CelestialLock;

public static class StatusMonitorSetup
{
    [MenuItem("Tools/Ship/Validate Status Monitor")]
    public static void Validate()
    {
        var monitor = Object.FindFirstObjectByType<StatusMonitor>();
        var serialized = new SerializedObject(monitor);
        string[] keys = { "zodiac", "wires", "password", "levers", "celestialLock" };
        var boards = new object[5];
        var original = new object[5];
        var fields = new System.Reflection.FieldInfo[5];
        try
        {
            for (int i = 0; i < 5; i++)
            {
                boards[i] = serialized.FindProperty(keys[i]).objectReferenceValue;
                fields[i] = boards[i].GetType().GetField("puzzle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                original[i] = fields[i].GetValue(boards[i]);
                fields[i].SetValue(boards[i], null);
            }
            monitor.Refresh();
            CheckRows(serialized, -1);
            for (int i = 0; i < 5; i++)
            {
                object solved;
                switch (i)
                {
                    case 0:
                        var zodiac = new LightSequencePuzzle(new[] { 1, 2 });
                        zodiac.Press(1); zodiac.Press(2); solved = zodiac; break;
                    case 1:
                        var wires = new WiresPuzzle(new[] { "a", "b", "c", "d", "e", "f", "g", "h" },
                            new[] { new WiresPair("a", "b"), new WiresPair("c", "d"), new WiresPair("e", "f"), new WiresPair("g", "h") });
                        for (int j = 0; j < 8; j++) wires.TryConnect(j, j);
                        solved = wires; break;
                    case 2:
                        var password = new SymbolPasswordPuzzle(new[] { 1 });
                        password.Enter(1); password.Submit(); solved = password; break;
                    case 3:
                        var levers = new LeversPuzzle(new[] { 2, 2, 2 });
                        for (int j = 0; j < 3; j++) levers.SetPosition(j, 2);
                        solved = levers; break;
                    default:
                        var celestialLock = new CelestialLockPuzzle(new[] { 0, 0, 0, 0 }, new[] { 0, 0, 0, 0 });
                        celestialLock.Submit(); solved = celestialLock; break;
                }
                fields[i].SetValue(boards[i], solved);
                monitor.Refresh();
                CheckRows(serialized, i);
                fields[i].SetValue(boards[i], null);
                monitor.Refresh();
                CheckRows(serialized, -1);
            }
        }
        finally
        {
            for (int i = 0; i < 5; i++) if (fields[i] != null) fields[i].SetValue(boards[i], original[i]);
            monitor.Refresh();
        }
        Canvas.ForceUpdateCanvases();
        var camera = monitor.transform.Find("Display camera").GetComponent<Camera>();
        camera.Render();
        var rt = camera.targetTexture;
        RenderTexture.active = rt;
        var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        image.Apply();
        File.WriteAllBytes("Temp/MonitorContent.png", image.EncodeToPNG());
        Object.DestroyImmediate(image);
        RenderTexture.active = null;
        Capture(Camera.main, "Temp/MonitorAfter.png");
        File.WriteAllText("Temp/MonitorValidation.txt", "PASS: root object; all five board references; independent FAIL to OK transitions; reset to FAIL; exact #333333 names; red failures and green successes. Unity render captured.");
        Debug.Log(File.ReadAllText("Temp/MonitorValidation.txt"));
    }

    private static void CheckRows(SerializedObject monitor, int completed)
    {
        if (((StatusMonitor)monitor.targetObject).transform.parent != null) throw new System.Exception("Monitor must be at the scene root.");
        for (int i = 0; i < 5; i++)
        {
            var label = (Text)monitor.FindProperty("statuses").GetArrayElementAtIndex(i).objectReferenceValue;
            if (label.text != (i == completed ? "OK" : "FAIL")) throw new System.Exception("Wrong row state: " + i);
            var expected = monitor.FindProperty(i == completed ? "okColor" : "failColor").colorValue;
            if (label.color != expected) throw new System.Exception("Wrong status color: " + i);
            var name = (Text)monitor.FindProperty("names").GetArrayElementAtIndex(i).objectReferenceValue;
            if (name.color != new Color(.2f, .2f, .2f)) throw new System.Exception("Names must use #333333.");
        }
    }
    [MenuItem("Tools/Ship/Assemble Status Monitor")]
    public static void Assemble()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/ControllerScene.unity")
            throw new System.InvalidOperationException("Open ControllerScene in Edit Mode first.");
        if (Object.FindFirstObjectByType<StatusMonitor>() != null)
            throw new System.InvalidOperationException("StatusMonitor already exists; edit the existing object.");
        const string folder = "Assets/Art/StatusMonitor";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var source = GameObject.Find("ShipPanel/Painel/pCylinder4").GetComponent<MeshFilter>();
        var mesh = Object.Instantiate(source.sharedMesh);
        mesh.name = "CurvedMonitorGlass";
        var vertices = mesh.vertices;
        var uv = new Vector2[vertices.Length];
        var bounds = mesh.bounds;
        for (int i = 0; i < vertices.Length; i++)
            uv[i] = new Vector2(Mathf.InverseLerp(bounds.min.x, bounds.max.x, vertices[i].x),
                Mathf.InverseLerp(bounds.min.z, bounds.max.z, vertices[i].z));
        mesh.uv = uv;
        AssetDatabase.CreateAsset(mesh, folder + "/CurvedMonitorGlass.asset");
        var texture = new RenderTexture(1024, 640, 24) { name = "StatusMonitorDisplay", antiAliasing = 1 };
        AssetDatabase.CreateAsset(texture, folder + "/StatusMonitorDisplay.renderTexture");
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.name = "M_StatusMonitorGlass";
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        AssetDatabase.CreateAsset(material, folder + "/M_StatusMonitorGlass.mat");

        var root = new GameObject("StatusMonitor");
        Undo.RegisterCreatedObjectUndo(root, "Assemble Status Monitor");
        var screen = new GameObject("Curved glass", typeof(MeshFilter), typeof(MeshRenderer));
        screen.transform.SetParent(root.transform, false);
        screen.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        screen.transform.localScale = source.transform.lossyScale;
        screen.GetComponent<MeshFilter>().sharedMesh = mesh;
        screen.GetComponent<MeshRenderer>().sharedMaterial = material;
        screen.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        source.GetComponent<Renderer>().enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(source.GetComponent<Renderer>());

        // This editable canvas is filmed remotely; only the curved glass appears in the cockpit.
        var content = new GameObject("Display content", typeof(RectTransform), typeof(Canvas));
        content.layer = 5;
        var rect = content.GetComponent<RectTransform>();
        rect.SetParent(root.transform, false);
        rect.position = new Vector3(10000, 0, 0);
        rect.localScale = Vector3.one * .001f;
        rect.sizeDelta = new Vector2(1024, 640);
        content.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var cameraObject = new GameObject("Display camera", typeof(Camera));
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.position = new Vector3(10000, 0, -2);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = .32f;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 3;
        camera.cullingMask = 1 << 5;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.72f, .80f, .69f);
        camera.targetTexture = texture;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.depth = -10;
        content.GetComponent<Canvas>().worldCamera = camera;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        string[] titles = { "Zodiac", "Wires", "Password", "Levers", "Lock" };
        var names = new Text[5];
        var statuses = new Text[5];
        for (int i = 0; i < 5; i++)
        {
            float y = 80 - i * 68;
            names[i] = Label(titles[i], rect, new Vector2(-112, y), new Vector2(470, 80), font, new Color(.2f, .2f, .2f));
            statuses[i] = Label("FAIL", rect, new Vector2(274, y), new Vector2(170, 80), font, new Color(.75f, .045f, .035f));
            statuses[i].name = titles[i] + " status";
            statuses[i].alignment = TextAnchor.MiddleRight;
        }
        var monitor = root.AddComponent<StatusMonitor>();
        var serialized = new SerializedObject(monitor);
        Set(serialized, "font", font);
        Set(serialized, "displayCamera", camera);
        Set(serialized, "zodiac", Object.FindFirstObjectByType<LightSequenceBoard>());
        Set(serialized, "wires", Object.FindFirstObjectByType<WiresBoard>());
        Set(serialized, "password", Object.FindFirstObjectByType<SymbolPasswordBoard>());
        Set(serialized, "levers", Object.FindFirstObjectByType<LeversBoard>());
        Set(serialized, "celestialLock", Object.FindFirstObjectByType<CelestialLockBoard>());
        for (int i = 0; i < 5; i++)
        {
            serialized.FindProperty("names").GetArrayElementAtIndex(i).objectReferenceValue = names[i];
            serialized.FindProperty("statuses").GetArrayElementAtIndex(i).objectReferenceValue = statuses[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        monitor.Refresh();
        Canvas.ForceUpdateCanvases();
        camera.Render();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Capture(Camera.main, "Temp/MonitorAfter.png");
        Selection.activeGameObject = root;
        Debug.Log("StatusMonitor assembled: curved glass, five rows and live board states.");
    }

    private static void Set(SerializedObject target, string field, Object value) => target.FindProperty(field).objectReferenceValue = value;

    private static Text Label(string title, Transform parent, Vector2 position, Vector2 size, Font font, Color color)
    {
        var label = new GameObject(title, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        label.gameObject.layer = 5;
        label.transform.SetParent(parent, false);
        label.rectTransform.sizeDelta = size;
        label.rectTransform.anchoredPosition = position;
        label.font = font;
        label.fontSize = 64;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = color;
        label.text = title;
        label.raycastTarget = false;
        return label;
    }
    public static void Capture(Camera camera, string path)
    {
        var target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(target);
    }
}
