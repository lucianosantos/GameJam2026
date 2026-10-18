using System;
using System.IO;
using System.Reflection;
using GameJam2026;
using GameJam2026.Levers;
using GameJam2026.SymbolPassword;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class RequestedAdjustmentsPlayChecks
{
    const string Pending = "RequestedAdjustments.PlayChecks";
    static int frames;
    static double nextStep;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Read(object o, string field) => o.GetType().GetField(field, Private).GetValue(o);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    [MenuItem("Tools/Validation/Check Requested Adjustments %#F8")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Start in Edit Mode.");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Register() => EditorApplication.playModeStateChanged += state =>
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false); frames = 0; nextStep = EditorApplication.timeSinceStartup + 1;
        EditorApplication.update += Step;
    };

    static void Capture(string name)
    {
        var overlays = Array.FindAll(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None), c => c.renderMode == RenderMode.ScreenSpaceOverlay);
        foreach (var c in overlays) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = Camera.main; c.planeDistance = .5f; }
        Canvas.ForceUpdateCanvases();
        StatusMonitorSetup.Capture(Camera.main, "Temp/" + name + ".png");
        foreach (var c in overlays) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
    }

    static void Write(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target,value);
    static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target,null);
    static void CheckFinalResult()
    {
        var session = Object.FindFirstObjectByType<GameSession>();
        Check(!session.TryFinishGame(), "Incomplete systems reject completion");
        var z = new GameJam2026.LightSequence.LightSequencePuzzle(new[]{1}); z.Press(1);
        Write(Object.FindFirstObjectByType<GameJam2026.LightSequence.LightSequenceBoard>(),"puzzle",z);
        var w = new GameJam2026.Wires.WiresPuzzle(new[]{"a","b","c","d","e","f","g","h"},
            new[]{new GameJam2026.Wires.WiresPair("a","b"),new GameJam2026.Wires.WiresPair("c","d"),new GameJam2026.Wires.WiresPair("e","f"),new GameJam2026.Wires.WiresPair("g","h")});
        for(int i=0;i<8;i++) w.TryConnect(i,i);
        Write(Object.FindFirstObjectByType<GameJam2026.Wires.WiresBoard>(),"puzzle",w);
        var p = new SymbolPasswordPuzzle(new[]{1}); p.Enter(1);p.Submit();
        Write(Object.FindFirstObjectByType<SymbolPasswordBoard>(),"puzzle",p);
        var l = new LeversPuzzle(new[]{4,4,2}); l.SetPosition(0,4);l.SetPosition(1,4);l.SetPosition(2,2);
        Write(Object.FindFirstObjectByType<LeversBoard>(),"puzzle",l);
        var c = new GameJam2026.CelestialLock.CelestialLockPuzzle(new[]{0,0,0,0},new[]{0,0,0,0});c.Submit();
        var lockBoard = Object.FindFirstObjectByType<GameJam2026.CelestialLock.CelestialLockBoard>();
        Write(lockBoard,"puzzle",c);
        var monitor = Object.FindFirstObjectByType<StatusMonitor>(); monitor.Refresh();
        Check(((Text)Read(monitor,"completionPrompt")).enabled,"Final instruction appears for all five completed systems");
        Write(lockBoard,"puzzle",null);monitor.Refresh();
        Check(!((Text)Read(monitor,"completionPrompt")).enabled,"Reset hides final instruction");
        Write(lockBoard,"puzzle",c);monitor.Refresh();
        var controller = Camera.main.GetComponent<CameraController>();controller.ReturnToOverview();
        Call(session,"CheckCompletion");
        var button = Object.FindFirstObjectByType<ShipCompletionButton>(); Call(button,"LateUpdate");Physics.SyncTransforms();
        var point = Camera.main.WorldToScreenPoint(button.GetComponent<Renderer>().bounds.center);
        Check(button.TryPressAtScreenPoint(Camera.main,point) && session.HasWon,"Physical completion button wins");
        Check(((Text)Read(session,"resultText")).color==Color.white,"White success heading");
        Check(((Text)Read(session,"resultMessage")).color==new Color(.48f,.86f,.62f),"Green victory phrase");
        Check(((Button)Read(session,"replayButton")).GetComponentInChildren<Text>().color==Color.white,"White button text");
        var late = new GameObject("Deadline validation",typeof(CameraController),typeof(GameSession));
        var lateSession=late.GetComponent<GameSession>();
        foreach(var field in new[]{"result","victoryContent","defeatContent"}) Write(lateSession,field,Read(session,field));
        Call(lateSession,"Start");
        ((CountdownClock)Read(lateSession,"clock")).Advance(600);
        Check(!lateSession.TryFinishGame() && lateSession.HasEnded && !lateSession.HasWon,"Deadline takes precedence");
        Check(((Text)Read(lateSession,"resultMessage")).color==new Color(1,.33f,.23f) &&
            ((Text)Read(lateSession,"resultText")).color==new Color(1,.33f,.23f),"Defeat text is red");
        ((GameObject)Read(session,"victoryContent")).SetActive(true);
        ((GameObject)Read(session,"defeatContent")).SetActive(false);
        ((GameObject)Read(session,"result")).SetActive(true);
        Object.Destroy(late);
    }

    static void Step()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Step; return; }
        if (EditorApplication.isPaused || EditorApplication.timeSinceStartup < nextStep) return;
        nextStep = EditorApplication.timeSinceStartup + .1;
        try
        {
            frames++;
            var controller = Camera.main.GetComponent<CameraController>();
            controller.GetType().GetField("moveDuration", Private).SetValue(controller, 0f);
            if (frames == 10)
            {
                var board = Object.FindFirstObjectByType<SymbolPasswordBoard>();
                var monitor = (Image)Read(board, "monitor");
                for (int i = 0; i < 5; i++) Check(monitor.transform.Find("Character underline " + (i+1)).GetComponent<Image>().enabled, "Five visible character underlines inside Monitor");
                controller.MoveTo((Transform)Read(board, "cameraTarget"));
            }
            if (frames == 15) { Capture("AdjustedPassword"); controller.MoveTo(GameObject.Find("CameraPositions/Wires").transform); }
            if (frames == 20)
            {
                Capture("AdjustedWires");
                var board = Object.FindFirstObjectByType<LeversBoard>();
                var solution = (int[])Read(board,"correctPositions");
                Check(solution[0]==4 && solution[1]==4 && solution[2]==2, "Authored solution 4,4,2");
                controller.MoveTo((Transform)Read(board,"cameraTarget"));
                var handles = (LeverHandle[])Read(board,"levers");
                board.SetPosition(handles[0],4); board.SetPosition(handles[1],4);
                Check(!board.IsCompleted,"Two levers alone cannot complete");
                board.SetPosition(handles[2],2); Check(board.IsCompleted,"4,4,2 completes the live board");
                board.RestartPuzzle();
                CheckFinalResult();
                File.WriteAllText("Temp/RequestedAdjustmentsPlayValidation.txt", "PASS: Unity Play Mode; five editable underlines; actual lever solution 4,4,2; final instruction when all five puzzles complete; physical completion button; white and green victory; red defeat; timeout priority.");
            }
            if (frames == 25) Capture("AdjustedVictoryPlayMode");
            if (frames == 30)
            {
                Debug.Log(File.ReadAllText("Temp/RequestedAdjustmentsPlayValidation.txt"));
                EditorApplication.update -= Step;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception e)
        {
            File.WriteAllText("Temp/RequestedAdjustmentsPlayValidation.txt", "FAIL: " + e);
            Debug.LogException(e); EditorApplication.update -= Step; EditorApplication.ExitPlaymode();
        }
    }
}

