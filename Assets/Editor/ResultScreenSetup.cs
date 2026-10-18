using System;
using System.IO;
using GameJam2026;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ResultScreenSetup
{
    [MenuItem("Tools/Ship/Author Result Screen %#F6")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/ControllerScene.unity") throw new InvalidOperationException("Open ControllerScene first.");
        var session = Object.FindFirstObjectByType<GameSession>();
        var data = new SerializedObject(session);
        var result = (GameObject)data.FindProperty("result").objectReferenceValue;
        if (result == null)
        {
            result = new GameObject("ResultScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(result,"Author result screen");
            var canvas = result.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1100;
            var scaler = result.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080);
            scaler.matchWidthOrHeight = .5f;
            var backdrop = Rect("Backdrop",result.transform,Vector2.zero,Vector2.zero);
            Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0,0,0,.94f);
            var victory = Content("Victory",result.transform,true);
            var defeat = Content("Defeat",result.transform,false);
            data.FindProperty("result").objectReferenceValue=result;
            data.FindProperty("victoryContent").objectReferenceValue=victory;
            data.FindProperty("defeatContent").objectReferenceValue=defeat;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        result.SetActive(false);
        var status = Object.FindFirstObjectByType<StatusMonitor>();
        var prompt = status.transform.Find("Display content/Completion prompt").GetComponent<Text>();
        prompt.fontSize=46;
        prompt.rectTransform.sizeDelta=new Vector2(960,76);
        var symbol = GameObject.Find("Symbol_fio_musica");
        symbol.GetComponent<MeshFilter>().sharedMesh=GameObject.Find("Symbol_fio_x").GetComponent<MeshFilter>().sharedMesh;
        symbol.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Minigames/Wires/Materials/Symbol_fio_musica.mat");
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=result;
        CaptureVariants(result, (GameObject)data.FindProperty("victoryContent").objectReferenceValue,
            (GameObject)data.FindProperty("defeatContent").objectReferenceValue);
        Debug.Log("ResultScreen authored at scene root: Victory/Defeat, Title 88, Subtitle 56, buttons 30. Monitor prompt 46. Black ink music tile restored.");
    }

    static void CaptureVariants(GameObject result, GameObject victory, GameObject defeat)
    {
        var canvas = result.GetComponent<Canvas>();
        try
        {
            result.SetActive(true);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = .5f;
            foreach (bool won in new[] { true, false })
            {
                victory.SetActive(won); defeat.SetActive(!won);
                Canvas.ForceUpdateCanvases();
                StatusMonitorSetup.Capture(Camera.main, "Temp/Authored" + (won ? "Victory" : "Defeat") + ".png");
            }
        }
        finally
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
            victory.SetActive(true); defeat.SetActive(false); result.SetActive(false);
            EditorSceneManager.SaveScene(result.scene);
        }
    }

    static GameObject Content(string name,Transform parent,bool won)
    {
        var group=Rect(name,parent,Vector2.zero,Vector2.zero);Stretch(group);
        Color color=won ? Color.white : new Color(1,.33f,.23f);
        Label("Title",won?"Congratulations!":"Time's Up",group,new Vector2(1500,128),new Vector2(0,150),88,color,FontStyle.Bold);
        Label("Subtitle",won?"You won the game!":"The ship has crashed",group,new Vector2(1500,88),new Vector2(0,38),56,
            won?new Color(.48f,.86f,.62f):color,FontStyle.Normal);
        Button("Replay",won?"Play Again":"Try Again",group,new Vector2(0,-112),color,won);
        Button("Menu","Back to Menu",group,new Vector2(0,-208),color,won);
        group.gameObject.SetActive(won);
        return group.gameObject;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false);rect.sizeDelta=size;rect.anchoredPosition=position;
        return rect;
    }
    static void Stretch(RectTransform rect)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static void Label(string name,string value,Transform parent,Vector2 size,Vector2 position,int fontSize,Color color,FontStyle style)
    {
        var text=Rect(name,parent,size,position).gameObject.AddComponent<Text>();
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.fontSize=fontSize;
        text.color=color;text.alignment=TextAnchor.MiddleCenter;text.fontStyle=style;text.raycastTarget=false;
    }
    static void Button(string name,string value,Transform parent,Vector2 position,Color color,bool won)
    {
        var rect=Rect(name,parent,new Vector2(440,76),position);
        var image=rect.gameObject.AddComponent<Image>();image.color=won?new Color(.07f,.12f,.10f):new Color(.19f,.075f,.06f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
        Label("Label",value,rect,rect.sizeDelta,Vector2.zero,30,color,FontStyle.Normal);
    }
}
