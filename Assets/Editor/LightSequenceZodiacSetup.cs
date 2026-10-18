using System;
using System.Linq;
using GameJam2026.LightSequence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LightSequenceZodiacSetup
{
    private const string Art = "Assets/Art/Minigames/LightSequence";
    private const string Prefab = "Assets/Prefabs/LightSequenceButton.prefab";
    // The manual's month order, filtered to the eight symbols in its handwritten code.
    private static readonly string[] Signs = { "PEIXES", "ARIES", "GEMEOS", "CANCER", "LEAO", "VIRGEM", "SAGITARIO", "CAPRICORNIO" };
    private static readonly int[] Solution = { 8, 2, 1, 4, 6, 7, 3, 5 };

    [MenuItem("Tools/Minigames/Light Sequence/Apply Zodiac Symbols")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var sprites = Signs.Select(ImportSymbol).ToArray();
        var template = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            ConfigureLegend(template.GetComponent<LightSequenceButton>(), sprites[0]);
            PrefabUtility.SaveAsPrefabAsset(template, Prefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(template); }

        var board = UnityEngine.Object.FindFirstObjectByType<LightSequenceBoard>();
        if (board == null) throw new InvalidOperationException("Open ControllerScene first.");
        var buttons = board.GetComponentsInChildren<LightSequenceButton>();
        if (buttons.Length != 8 || buttons.Select(b => b.ButtonId).Distinct().Count() != 8)
            throw new InvalidOperationException("Expected eight unique keys.");
        foreach (var button in buttons)
        {
            if (button.ButtonId < 1 || button.ButtonId > 8) throw new InvalidOperationException("Expected IDs 1 through 8.");
            ConfigureLegend(button, sprites[button.ButtonId - 1]);
        }
        var so = new SerializedObject(board);
        var order = so.FindProperty("correctOrder"); order.arraySize = Solution.Length;
        for (int i = 0; i < Solution.Length; i++) order.GetArrayElementAtIndex(i).intValue = Solution[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(board);
        EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(board.gameObject.scene);
        Debug.Log("Zodiac keys applied. Manual 12,3,2,6,8,11,5,7 => button IDs 8,2,1,4,6,7,3,5.");
    }

    private static Sprite ImportSymbol(string sign)
    {
        string path = Art + "/" + sign + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = true;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var pixels = texture.GetPixels32();
        int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
        for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
                if (pixels[y * texture.width + x].a > 10)
                { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
        if (maxX < minX) throw new InvalidOperationException("Empty zodiac image: " + sign);
        minX = Math.Max(0, minX - 2); minY = Math.Max(0, minY - 2);
        maxX = Math.Min(texture.width - 1, maxX + 2); maxY = Math.Min(texture.height - 1, maxY + 2);
        // A separate Sprite asset trims transparent margins without changing the supplied PNG.
        string spritePath = Art + "/Zodiac_" + sign + ".asset";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            sprite = Sprite.Create(texture, new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = sign;
            AssetDatabase.CreateAsset(sprite, spritePath);
        }
        importer.isReadable = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    private static void ConfigureLegend(LightSequenceButton button, Sprite sprite)
    {
        var child = button.transform.Find("ZodiacSymbol");
        if (child == null)
        {
            child = new GameObject("ZodiacSymbol", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
            child.SetParent(button.transform, false);
        }
        var rect = (RectTransform)child;
        rect.localPosition = new Vector3(0, 0, -.021f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(.043f, .043f);
        var image = child.GetComponent<Image>();
        image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; image.color = Color.white;
        var so = new SerializedObject(button);
        var legends = so.FindProperty("legends"); legends.arraySize = 1;
        legends.GetArrayElementAtIndex(0).objectReferenceValue = child;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(button))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }
    }
}
