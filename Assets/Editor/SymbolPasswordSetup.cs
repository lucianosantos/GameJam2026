using System;
using System.IO;
using GameJam2026.SymbolPassword;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SymbolPasswordSetup
{
    private const string Art = "Assets/Art/Minigames/SymbolPassword";
    private const string PrefabPath = "Assets/Prefabs/SymbolPasswordButton.prefab";

    [MenuItem("Tools/Minigames/Symbol Password/Assemble")]
    public static void Assemble()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var panel = GameObject.Find("Minigames/SymbolPassword/Canvas").GetComponent<RectTransform>();
        if (panel.GetComponent<SymbolPasswordBoard>() != null) throw new InvalidOperationException("Already assembled; edit the existing scene and prefab.");
        var source = panel.Find("BtnSenha");
        if (source == null) throw new InvalidOperationException("Expected the authored BtnSenha model.");
        var mesh = source.GetComponentInChildren<MeshFilter>().sharedMesh;
        Directory.CreateDirectory(Art + "/Materials");
        AssetDatabase.Refresh();
        var symbols = new Sprite[10];
        for (int digit = 0; digit < 10; digit++)
        {
            string path = Art + "/Textures/LINHA " + (digit == 0 ? 10 : digit) + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            symbols[digit] = TrimSymbol(path, digit);
        }
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = "M_SymbolPasswordButton";
        material.SetColor("_BaseColor", new Color(.72f, .74f, .69f));
        material.SetFloat("_Smoothness", .3f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(.001f, .001f, .001f));
        AssetDatabase.CreateAsset(material, Art + "/Materials/M_SymbolPasswordButton.mat");

        var template = new GameObject("SymbolPasswordButton");
        var visual = new GameObject("Visual", typeof(MeshFilter), typeof(MeshRenderer));
        visual.transform.SetParent(template.transform, false);
        visual.GetComponent<MeshFilter>().sharedMesh = mesh;
        visual.GetComponent<MeshRenderer>().sharedMaterial = material;
        visual.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        var size = mesh.bounds.size;
        visual.transform.localScale = new Vector3(.052f / size.x, .018f / size.y, .035f / size.z);
        visual.transform.localPosition = -(visual.transform.localRotation * Vector3.Scale(mesh.bounds.center, visual.transform.localScale));
        var collider = template.AddComponent<BoxCollider>();
        collider.size = new Vector3(.058f, .040f, .024f);
        var key = template.AddComponent<SymbolPasswordButton>();
        Set(key, "visual", visual.transform); Set(key, "keyRenderer", visual.GetComponent<Renderer>());
        var symbol = Image("Symbol", template.transform, new Vector2(0, -.006f), new Vector2(.046f, .020f), Color.white);
        symbol.sprite = symbols[1]; symbol.preserveAspect = true;
        var label = Text("Label", template.transform, new Vector2(0, -.006f), new Vector2(.046f, .020f), "");
        label.fontSize = 14;
        SetArray(key, "legends", new UnityEngine.Object[] { symbol.transform, label.transform });
        label.gameObject.SetActive(false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
        UnityEngine.Object.DestroyImmediate(template);

        var board = panel.gameObject.AddComponent<SymbolPasswordBoard>();
        var buttons = new SymbolPasswordButton[12];
        for (int index = 0; index < buttons.Length; index++)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, panel);
            instance.transform.localPosition = new Vector3((index % 3 - 1) * .085f, .052f - (index / 3) * .070f, -.02f);
            var button = instance.GetComponent<SymbolPasswordButton>(); buttons[index] = button;
            var action = index == 9 ? PasswordKeyAction.Clear : index == 11 ? PasswordKeyAction.Submit : PasswordKeyAction.Digit;
            int digit = index == 10 ? 0 : index + 1;
            Set(button, "action", (int)action); Set(button, "digit", action == PasswordKeyAction.Digit ? digit : 0);
            var image = instance.transform.Find("Symbol").GetComponent<Image>();
            image.gameObject.SetActive(action == PasswordKeyAction.Digit);
            if (action == PasswordKeyAction.Digit) image.sprite = symbols[digit];
            var text = instance.transform.Find("Label").GetComponent<Text>();
            text.gameObject.SetActive(action != PasswordKeyAction.Digit);
            text.text = action == PasswordKeyAction.Clear ? "Clear" : "Ok";
            instance.name = action == PasswordKeyAction.Digit ? "SymbolKey_" + digit : action.ToString();
        }
        var monitor = Image("Monitor", panel, new Vector2(0, .124f), new Vector2(.264f, .055f), new Color(.72f, .84f, .70f));
        var slots = new Image[5];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = Image("Slot_" + (i + 1), monitor.transform, new Vector2((i - 2) * .05f, 0), new Vector2(.042f, .039f), Color.white);
            slots[i].preserveAspect = true; slots[i].enabled = false;
        }
        var camera = Camera.main;
        var controller = camera.GetComponent<CameraController>();
        var target = new GameObject("SymbolPassword").transform;
        target.SetParent(GameObject.Find("CameraPositions").transform, false);
        float distance = .175f * 1.12f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
        target.SetPositionAndRotation(panel.TransformPoint(new Vector3(0, .006f, -distance)), panel.rotation);
        Set(board, "cameraController", controller); Set(board, "cameraTarget", target);
        SetArray(board, "buttons", buttons); SetArray(board, "digitSymbols", symbols); SetArray(board, "displaySlots", slots); Set(board, "monitor", monitor);
        var area = new GameObject("SymbolPasswordClickArea", typeof(BoxCollider), typeof(CameraClickArea));
        area.transform.SetParent(panel, false); area.transform.localPosition = new Vector3(0, -.012f, -.055f);
        area.GetComponent<BoxCollider>().size = new Vector3(.30f, .346f, .012f);
        Set(area.GetComponent<CameraClickArea>(), "cameraController", controller);
        Set(area.GetComponent<CameraClickArea>(), "targetPosition", target);
        area.transform.SetParent(GameObject.Find("ShipPanel").transform, true);
        var raycaster = panel.GetComponent<GraphicRaycaster>();
        if (raycaster != null) raycaster.enabled = false;
        if (camera.GetComponent<PhysicsRaycaster>() == null) camera.gameObject.AddComponent<PhysicsRaycaster>();
        UnityEngine.Object.DestroyImmediate(source.gameObject);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(panel.gameObject.scene);
        Debug.Log("Symbol Password assembled: 12 keys, 5 symbol display slots, fixed solution 41036, editable prefab and OnCompleted.");
    }

    private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition3D = new Vector3(position.x, position.y, -.035f);
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    private static Text Text(string name, Transform parent, Vector2 position, Vector2 size, string value)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.sizeDelta = size; rect.localScale = Vector3.one * .001f; rect.sizeDelta = size * 1000;
        rect.localPosition = new Vector3(position.x, position.y, -.035f);
        var text = go.GetComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 17; text.alignment = TextAnchor.MiddleCenter; text.color = Color.black; text.raycastTarget = false;
        return text;
    }
    // Use the supplied PNG unchanged; only the Sprite rectangle excludes transparent padding.
    public static Sprite TrimSymbol(string texturePath, int digit)
    {
        string spritePath = Art + "/Symbol_" + digit + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (existing != null) return existing;
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.isReadable = true; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var pixels = texture.GetPixels32();
        int minX = texture.width, minY = texture.height, maxX = 0, maxY = 0;
        for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
                if (pixels[y * texture.width + x].a > 10)
                { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
        minX = Mathf.Max(0, minX - 2); minY = Mathf.Max(0, minY - 2);
        maxX = Mathf.Min(texture.width - 1, maxX + 2); maxY = Mathf.Min(texture.height - 1, maxY + 2);
        var sprite = Sprite.Create(texture, new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        sprite.name = "Symbol_" + digit;
        AssetDatabase.CreateAsset(sprite, spritePath);
        importer.isReadable = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }
    private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    { var so = new SerializedObject(target); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Set(UnityEngine.Object target, string name, int value)
    { var so = new SerializedObject(target); so.FindProperty(name).intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void SetArray(UnityEngine.Object target, string name, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(target); var array = so.FindProperty(name); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
