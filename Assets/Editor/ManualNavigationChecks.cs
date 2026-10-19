using System;
using System.Reflection;
using GameJam2026;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ManualNavigationChecks
{
    [MenuItem("Tools/Validation/Check Manual Navigation")]
    public static void Run()
    {
        // Preview scene keeps the currently edited scenes and their unsaved changes intact.
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/ManualScene.unity");
        try
        {
            ManualNavigation navigation = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                navigation = root.GetComponentInChildren<ManualNavigation>();
                if (navigation != null) break;
            }
            Check(navigation != null, "Manual controller is assigned");
            var image = (Image)Read(navigation, "pageImage");
            var pages = (Sprite[])Read(navigation, "pages");
            var previous = (Button)Read(navigation, "previousButton");
            var next = (Button)Read(navigation, "nextButton");
            Check(pages.Length == 8, "Eight manual images are assigned");
            for (int i = 0; i < pages.Length; i++)
            {
                Check(pages[i] != null, "Page sprite is imported");
                Check(AssetDatabase.GetAssetPath(pages[i]) == $"Assets/Art/Manual/P{i + 1}.jpg", "Pages are ordered P1 through P8");
                Check(pages[i].texture.width == 2560 && pages[i].texture.height == 1440, "Original page resolution is preserved");
            }
            Check(image.sprite == pages[0] && !previous.interactable && next.interactable, "Scene starts on P1 with back disabled");
            PrepareClick(previous, navigation, "PreviousPage");
            PrepareClick(next, navigation, "NextPage");
            typeof(ManualNavigation).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(navigation, null);
            Click(previous);
            Check(image.sprite == pages[0], "Back click cannot leave first page");
            navigation.PreviousPage();
            Check(image.sprite == pages[0], "Direct back call cannot leave first page");
            for (int i = 1; i < pages.Length; i++)
            {
                Click(next);
                Check(image.sprite == pages[i] && previous.interactable && next.interactable == (i < pages.Length - 1), "Forward click and button states on P" + (i + 1));
            }
            Click(next);
            navigation.NextPage();
            Check(image.sprite == pages[7] && !next.interactable, "Last page rejects further advancement");
            for (int i = pages.Length - 2; i >= 0; i--)
            {
                Click(previous);
                Check(image.sprite == pages[i] && previous.interactable == (i > 0) && next.interactable, "Back click and button states on P" + (i + 1));
            }
            Debug.Log("PASS: Manual P1-P8 imports, original resolution, persistent arrow callbacks, full forward/back navigation and both boundaries.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static object Read(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Click(Button button) => button.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
    private static void PrepareClick(Button button, ManualNavigation target, string method)
    {
        Check(button.onClick.GetPersistentEventCount() == 1 && button.onClick.GetPersistentTarget(0) == target && button.onClick.GetPersistentMethodName(0) == method, "Arrow has the correct serialized callback");
        // Allow runtime callbacks in this isolated Edit Mode preview only; never save this scene.
        button.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
    }
}
