using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameJam2026.CelestialLock;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CelestialLockSetup
{
    private const string Art = "Assets/Art/Minigames/CelestialLock/";
    private static readonly string[] ConfirmedCipherWords = { "USL", "HQ²L", "UCVSZL", "UVCZ" };
    private static readonly CelestialSymbol[] ConfirmedOrder = { CelestialSymbol.Sun, CelestialSymbol.Moon, CelestialSymbol.Saturn, CelestialSymbol.Star };

    [MenuItem("Tools/Minigames/Celestial Lock/Configure Status Light")]
    public static void ConfigureStatusLight()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before configuring the status light.");
        var board = GameObject.Find("Minigames/CelestialLock").GetComponent<CelestialLockBoard>();
        var finalLight = GameObject.Find("ShipPanel/Painel/Luz_Cadeado");
        var light = finalLight != null ? finalLight.GetComponentInChildren<Renderer>() :
            GameObject.Find("ShipPanel").GetComponentsInChildren<Renderer>().Single(r => r.name == "pSphere5");
        const string path = "Assets/Art/Materials/M_CelestialLockStatus.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_LeversStatus.mat"));
            material.name = "M_CelestialLockStatus";
            AssetDatabase.CreateAsset(material, path);
        }
        var red = new Color(1f, .055f, .015f);
        material.SetColor("_BaseColor", red);
        material.SetColor("_EmissionColor", red * 2.4f);
        material.EnableKeyword("_EMISSION");
        Undo.RecordObject(light, "Assign celestial lock status light");
        light.sharedMaterial = material;
        light.SetPropertyBlock(null);
        Set(board, "statusLight", light);
        EditorUtility.SetDirty(material);
        EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(board.gameObject.scene);
        Debug.Log("CelestialLock status light: authored red, green on completion.");
    }

    [MenuItem("Tools/Minigames/Celestial Lock/Apply Confirmed Cipher Text")]
    public static void ApplyConfirmedCipherText()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before editing the saved cipher text.");
        var board = GameObject.Find("Minigames/CelestialLock").GetComponent<CelestialLockBoard>();
        var so = new SerializedObject(board);
        var words = so.FindProperty("cipherWords");
        var order = so.FindProperty("correctOrder");
        var lines = so.FindProperty("cipherLines");
        if (lines.arraySize != 4) throw new InvalidOperationException("Expected four cipher labels.");
        var labels = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            labels[i] = lines.GetArrayElementAtIndex(i).objectReferenceValue as Text;
            if (labels[i] == null) throw new InvalidOperationException("Missing cipher label " + i);
        }
        Undo.RecordObjects(new UnityEngine.Object[] { board, labels[0], labels[1], labels[2], labels[3] }, "Apply celestial cipher words");
        words.arraySize = 4;
        order.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            words.GetArrayElementAtIndex(i).stringValue = ConfirmedCipherWords[i];
            order.GetArrayElementAtIndex(i).intValue = (int)ConfirmedOrder[i];
            labels[i].text = ConfirmedCipherWords[i];
            EditorUtility.SetDirty(labels[i]);
        }
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        EditorSceneManager.SaveScene(board.gameObject.scene);
        ValidateAndCapture();
    }
    [MenuItem("Tools/Minigames/Celestial Lock/Rebuild Symbol Texture")]
    public static void RebuildSymbolTexture()
    {
        var texture = SymbolTexture();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "Materials/DrumSymbols.mat");
        if (material != null) { material.SetTexture("_BaseMap", texture); EditorUtility.SetDirty(material); }
        AssetDatabase.SaveAssets();
    }
    public static void AssembleBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ControllerScene.unity");
        Assemble();
        ValidateAndCapture();
    }

    [MenuItem("Tools/Minigames/Celestial Lock/Assemble")]
    public static void Assemble()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/ControllerScene.unity") throw new InvalidOperationException("Open ControllerScene in Edit Mode.");
        var minigames = GameObject.Find("Minigames").transform;
        if (minigames.Find("CelestialLock") != null) throw new InvalidOperationException("CelestialLock already exists. Edit its scene objects and assets.");
        foreach (string folder in new[] { "Meshes", "Materials", "Textures", "Prefabs" }) Directory.CreateDirectory(Art + folder);
        AssetDatabase.Refresh();
        Mesh box = SaveMesh("BeveledBox", BeveledBox());
        Mesh drum = SaveMesh("FourSymbolDrum", Drum());
        Mesh pointer = SaveMesh("Pointer", Pointer());
        var dark = Material("Frame", new Color(.16f, .19f, .20f), .7f, .36f);
        var black = Material("Recess", new Color(.025f, .032f, .035f), .15f, .2f);
        var metal = Material("Steel", new Color(.58f, .61f, .6f), .7f, .4f);
        var cream = Material("CipherPlate", new Color(.84f, .82f, .69f), .12f, .2f);
        var symbols = Material("DrumSymbols", Color.white, .12f, .28f);
        symbols.SetTexture("_BaseMap", SymbolTexture());

        // Use the authored panel layout without depending on another minigame.
        var root = new GameObject("CelestialLock");
        Undo.RegisterCreatedObjectUndo(root, "Create Celestial Lock");
        root.transform.SetParent(minigames, false);
        root.transform.localPosition = new Vector3(0, .03f, 0);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        var canvas = go.GetComponent<RectTransform>();
        canvas.SetParent(root.transform, false);
        canvas.anchorMin = Vector2.zero; canvas.anchorMax = Vector2.zero; canvas.pivot = Vector2.one * .5f;
        canvas.sizeDelta = new Vector2(.9f, .9f); canvas.anchoredPosition3D = new Vector3(1.25f, .037f, -.324f);
        canvas.localRotation = new Quaternion(.6429108f, 0, 0, .7659411f); canvas.localScale = Vector3.one;
        var camera = Camera.main;
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        go.GetComponent<Canvas>().worldCamera = camera;
        var board = root.AddComponent<CelestialLockBoard>();
        // Geometry is authored at 0.9 x 0.9 and scales with the original Canvas area.
        var visual = new GameObject("LockAssembly").transform; visual.SetParent(canvas, false);
        visual.localScale = new Vector3(canvas.rect.width / .9f, canvas.rect.height / .9f, 1);
        Part("Backing", visual, box, dark, new Vector3(0, 0, .014f), new Vector3(.86f, .88f, .06f));
        Part("Recess", visual, box, black, new Vector3(0, .185f, -.025f), new Vector3(.75f, .39f, .04f));

        // Prefabs and mesh/material assets remain editable, as in Wires.
        var rollerTemplate = new GameObject("Roller");
        Part("Drum", rollerTemplate.transform, drum, symbols, Vector3.zero, Vector3.one);
        var rollerPrefab = PrefabUtility.SaveAsPrefabAsset(rollerTemplate, Art + "Prefabs/Roller.prefab");
        UnityEngine.Object.DestroyImmediate(rollerTemplate);
        Transform[] rollers = new Transform[4]; Collider[] hits = new Collider[4];
        for (int i = 0; i < 4; i++)
        {
            var rollerRoot = new GameObject("Roller_" + (i + 1)); rollerRoot.transform.SetParent(visual, false);
            rollerRoot.transform.localPosition = new Vector3((i - 1.5f) * .169f, .185f, -.007f);
            hits[i] = rollerRoot.AddComponent<BoxCollider>();
            ((BoxCollider)hits[i]).center = new Vector3(0, 0, -.11f);
            ((BoxCollider)hits[i]).size = new Vector3(.15f, .24f, .08f);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(rollerPrefab, rollerRoot.transform);
            rollers[i] = instance.transform;
            rollers[i].localRotation = Quaternion.Euler(-90f * (3 - i), 0, 0);
        }
        Part("UpperRollerGuard", visual, box, dark, new Vector3(0, .365f, -.073f), new Vector3(.78f, .07f, .10f));
        Part("LowerRollerGuard", visual, box, dark, new Vector3(0, .005f, -.073f), new Vector3(.78f, .055f, .10f));
        Part("LeftPointer", visual, pointer, metal, new Vector3(-.377f, .185f, -.158f), Vector3.one);
        var right = Part("RightPointer", visual, pointer, metal, new Vector3(.377f, .185f, -.158f), Vector3.one);
        right.transform.localRotation = Quaternion.Euler(0, 0, 180);
        Part("CipherBezel", visual, box, black, new Vector3(0, -.177f, -.024f), new Vector3(.77f, .30f, .035f));
        Part("CipherPlate", visual, box, cream, new Vector3(0, -.177f, -.045f), new Vector3(.735f, .275f, .018f));
        var labels = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            labels[i] = Label("CipherWord_" + (i + 1), visual, new Vector3(0, -.075f - .068f * i, -.057f), new Vector2(.64f, .055f), ConfirmedCipherWords[i], 34);
            if (i < 3) Part("Line_" + (i + 1), visual, box, dark, new Vector3(0, -.109f - .068f * i, -.056f), new Vector3(.64f, .001f, .001f));
        }
        Part("ConfirmBezel", visual, box, black, new Vector3(0, -.375f, -.035f), new Vector3(.31f, .088f, .045f));
        var button = new GameObject("ConfirmButton"); button.transform.SetParent(visual, false);
        button.transform.localPosition = new Vector3(0, -.375f, -.062f);
        var buttonHit = button.AddComponent<BoxCollider>(); buttonHit.size = new Vector3(.275f, .066f, .038f);
        var buttonVisual = new GameObject("MovingVisual").transform; buttonVisual.SetParent(button.transform, false);
        var buttonMesh = Part("MetalButton", buttonVisual, box, metal, Vector3.zero, new Vector3(.275f, .066f, .035f));
        Label("Label", buttonVisual, new Vector3(0, 0, -.02f), new Vector2(.25f, .05f), "CONFIRM", 28);
        foreach (float x in new[] { -.39f, .39f }) foreach (float y in new[] { -.405f, .405f })
        {
            Part("Screw", visual, box, metal, new Vector3(x, y, -.027f), new Vector3(.024f, .024f, .012f));
            Part("ScrewSlot", visual, box, black, new Vector3(x, y, -.034f), new Vector3(.014f, .0025f, .002f));
        }
        var controller = camera.GetComponent<CameraController>();
        var target = new GameObject("CelestialLock").transform; target.SetParent(GameObject.Find("CameraPositions").transform, false);
        float distance = .45f * 1.18f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2);
        target.SetPositionAndRotation(canvas.TransformPoint(new Vector3(0, 0, -distance)), canvas.rotation);
        Set(board, "cameraController", controller); Set(board, "cameraTarget", target); Set(board, "interactionCamera", camera);
        SetArray(board, "rollers", rollers); SetArray(board, "rollerColliders", hits); SetArray(board, "cipherLines", labels);
        Set(board, "confirmCollider", buttonHit); Set(board, "confirmVisual", buttonVisual); Set(board, "confirmRenderer", buttonMesh.GetComponent<Renderer>());
        var area = new GameObject("CelestialLockClickArea", typeof(BoxCollider), typeof(CameraClickArea));
        area.transform.SetParent(visual, false); area.transform.localPosition = new Vector3(0, 0, -.20f);
        area.GetComponent<BoxCollider>().size = new Vector3(.86f, .88f, .012f);
        Set(area.GetComponent<CameraClickArea>(), "cameraController", controller); Set(area.GetComponent<CameraClickArea>(), "targetPosition", target);
        area.transform.SetParent(GameObject.Find("ShipPanel").transform, true);
        ConfigureStatusLight();
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        Debug.Log("Celestial Lock assembled.");
    }

    private static GameObject Part(string name, Transform parent, Mesh mesh, Material material, Vector3 position, Vector3 scale)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material; return go;
    }
    private static Text Label(string name, Transform parent, Vector3 position, Vector2 size, string value, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false); rect.localPosition = position; rect.localScale = Vector3.one * .001f; rect.sizeDelta = size * 1000;
        var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Truncate;
        text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.055f, .065f, .065f); text.text = value; text.raycastTarget = false; return text;
    }
    private static Material Material(string name, Color color, float metallic, float smoothness)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.name = name;
        material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, Art + "Materials/" + name + ".mat"); return material;
    }
    private static Mesh SaveMesh(string name, Mesh mesh) { mesh.name = name; AssetDatabase.CreateAsset(mesh, Art + "Meshes/" + name + ".asset"); return mesh; }
    private static Mesh BeveledBox()
    {
        var v = new List<Vector3>(); var t = new List<int>();
        // Four rectangular rings form a chamfered solid; duplicate vertices per face for crisp normals.
        Vector3[][] rings = new Vector3[4][];
        for (int r = 0; r < 4; r++)
        {
            float size = r == 0 || r == 3 ? .44f : .5f;
            float z = r == 0 ? -.5f : r == 1 ? -.34f : r == 2 ? .34f : .5f;
            rings[r] = new[] { new Vector3(-size,-size,z), new Vector3(-size,size,z), new Vector3(size,size,z), new Vector3(size,-size,z) };
        }
        Action<Vector3,Vector3,Vector3,Vector3> quad = (a,b,c,d) => { int k=v.Count; v.AddRange(new[]{a,b,c,d}); t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3}); };
        quad(rings[0][0],rings[0][1],rings[0][2],rings[0][3]);
        for(int r=0;r<3;r++) for(int i=0;i<4;i++) { int j=(i+1)%4; quad(rings[r][j],rings[r][i],rings[r+1][i],rings[r+1][j]); }
        quad(rings[3][3],rings[3][2],rings[3][1],rings[3][0]);
        var mesh=new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(t,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    private static Mesh Drum()
    {
        var v=new List<Vector3>(); var uv=new List<Vector2>(); var t=new List<int>(); const int segments=96;
        for(int i=0;i<=segments;i++)
        {
            float a=(-45f+360f*i/segments)*Mathf.Deg2Rad;
            for(int side=0;side<2;side++) { v.Add(new Vector3((side-.5f)*.15f,Mathf.Sin(a)*.151f,-Mathf.Cos(a)*.151f)); uv.Add(new Vector2(side,i/(float)segments)); }
        }
        for(int i=0;i<segments;i++) { int k=i*2; t.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3}); }
        var mesh=new Mesh(); mesh.SetVertices(v); mesh.SetUVs(0,uv); mesh.SetTriangles(t,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    private static Mesh Pointer()
    {
        var m=new Mesh(); m.vertices=new[]{new Vector3(-.016f,-.019f,0),new Vector3(-.016f,.019f,0),new Vector3(.018f,0,0)};
        m.triangles=new[]{0,1,2}; m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }
    private static Texture2D SymbolTexture()
    {
        const int w=256,h=1024;
        var tex=new Texture2D(w,h,TextureFormat.RGB24,false); var pixels=new Color[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            int symbol=y/256; Vector2 p=new Vector2((x+.5f)/256-.5f,(((y%256)+.5f)/256-.5f) * (.151f * Mathf.PI / 2 / .15f));
            float ink=0;
            if(symbol==0) { bool disk=(p-new Vector2(-.015f,0)).magnitude<.30f; bool cut=(p-new Vector2(.13f,.10f)).magnitude<.285f; ink=disk&&!cut?1:0; }
            if(symbol==1) ink=Mathf.Sqrt(Mathf.Abs(p.x)/.32f)+Mathf.Sqrt(Mathf.Abs(p.y)/.35f)<1?1:0;
            if(symbol==2)
            {
                ink=p.magnitude<.18f?1:0;
                for(int ray=0;ray<8;ray++) { float a=ray*Mathf.PI/4; Vector2 axis=new Vector2(Mathf.Cos(a),Mathf.Sin(a)); float along=Vector2.Dot(p,axis); float across=Mathf.Abs(p.x*axis.y-p.y*axis.x); if(along>.23f&&along<.34f&&across<.019f) ink=1; }
            }
            if(symbol==3)
            {
                // A tilted ellipse makes Saturn distinct from both the moon and the sun.
                float a=.4f; Vector2 q=new Vector2(Mathf.Cos(a)*p.x+Mathf.Sin(a)*p.y,-Mathf.Sin(a)*p.x+Mathf.Cos(a)*p.y);
                float e=Mathf.Sqrt(q.x*q.x/(.39f*.39f)+q.y*q.y/(.125f*.125f));
                bool ring=Mathf.Abs(e-1)<.13f; bool planet=p.magnitude<.215f;
                bool gap=planet&&q.y<-.025f&&Mathf.Abs(e-1)<.32f;
                ink=ring||planet&&!gap?1:0;
            }
            bool edge=y%256<2||y%256>253||x<2||x>253;
            float noise=((x*17+y*31)%23)/23f*.025f;
            Color bg=new Color(.84f+noise,.82f+noise,.70f+noise);
            pixels[y*w+x]=ink>0?new Color(.045f,.055f,.058f):edge?new Color(.40f,.40f,.35f):bg;
        }
        tex.SetPixels(pixels); tex.Apply(); string path=Art+"Textures/DrumSymbols.png"; File.WriteAllBytes(path,tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path); var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static void Set(UnityEngine.Object target,string name,UnityEngine.Object value) {var so=new SerializedObject(target);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void SetArray(UnityEngine.Object target,string name,UnityEngine.Object[] values) {var so=new SerializedObject(target);var a=so.FindProperty(name);a.arraySize=values.Length;for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}

    public static void ValidateAndCapture()
    {
        var board=UnityEngine.Object.FindFirstObjectByType<CelestialLockBoard>();
        if(board==null || board.CameraTarget==null) throw new Exception("Missing lock or camera target.");
        var canvas=board.transform.Find("Canvas").GetComponent<RectTransform>();
        if(canvas.sizeDelta!=new Vector2(.9f,.9f)) throw new Exception("Unexpected Canvas area.");
        var so=new SerializedObject(board);
        foreach(string name in new[]{"rollers","rollerColliders","cipherLines"}) { var a=so.FindProperty(name); if(a.arraySize!=4)throw new Exception(name);for(int i=0;i<4;i++)if(a.GetArrayElementAtIndex(i).objectReferenceValue==null)throw new Exception(name+" missing reference"); }
        // Every full sequence has exactly one accepted submission for the confirmed solution.
        var order = so.FindProperty("correctOrder");
        var solution = new int[4];
        for (int i = 0; i < 4; i++) solution[i] = order.GetArrayElementAtIndex(i).intValue;
        int accepted=0;
        for(int n=0;n<256;n++) {var p=new CelestialLockPuzzle(solution,new[]{n%4,n/4%4,n/16%4,n/64%4});if(p.Submit())accepted++;}
        if(accepted!=1)throw new Exception("Solution is not unique.");
        var camera=Camera.main; var oldPos=camera.transform.position; var oldRot=camera.transform.rotation;
        camera.transform.SetPositionAndRotation(board.CameraTarget.position,board.CameraTarget.rotation);
        Canvas.ForceUpdateCanvases();
        var rt=new RenderTexture(1200,1000,24);var previous=camera.targetTexture;var active=RenderTexture.active;
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var texture=new Texture2D(1200,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1200,1000),0,0);texture.Apply();
        Directory.CreateDirectory("Docs/Images");File.WriteAllBytes("Docs/Images/CelestialLock-Prototype.png",texture.EncodeToPNG());
        camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);
        camera.transform.SetPositionAndRotation(oldPos,oldRot);
        Debug.Log("PASS Celestial Lock: Canvas area validated, references complete, all 256 combinations checked, prototype captured.");
    }
}
