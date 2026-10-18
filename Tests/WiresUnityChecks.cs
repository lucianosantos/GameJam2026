using System;
using System.IO;
using System.Reflection;
using GameJam2026.Wires;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Invoke via Pipeline run_script in Play Mode. Synthetic Input System events drive the real Update.
// No scene/asset saves. Stop Play Mode afterwards to restore the initial arrangement.
public static class WiresUnityChecks
{
    private static WiresBoard board;
    private static WiresCable[] cables;
    private static WiresSocket[] sockets;
    private static WiresPuzzle puzzle;
    private static Mouse mouse;
    private static Camera camera;
    private static Transform surface;
    private static object Read(object target, string name) => Field(target, name).GetValue(target);
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
    private static void Write(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Transform End(int endpoint) => cables[endpoint / 2].End(endpoint % 2);
    private static void Step(Vector3 world, bool pressed)
    {
        Physics.SyncTransforms();
        Vector2 screen = camera.WorldToScreenPoint(world);
        var state = new MouseState { position = screen }.WithButton(MouseButton.Left, pressed);
        InputSystem.QueueStateEvent(mouse, state); InputSystem.Update();
        typeof(WiresBoard).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(board, null);
        board.RefreshCableGeometry();
    }
    private static void Drag(int endpoint, Vector3 target)
    {
        Step(End(endpoint).position, false);
        Step(End(endpoint).position, true);
        Check((int)Read(board, "heldEndpoint") == endpoint, "Pointer must select the requested endpoint " + endpoint +
            "; held=" + Read(board,"heldEndpoint") + "; active=" + board.CanInteract + "; collider=" + cables[endpoint/2].GrabCollider(endpoint%2).enabled +
            "; pointer=" + mouse.position.ReadValue() + "; expected=" + camera.WorldToScreenPoint(End(endpoint).position) +
            "; pressed=" + mouse.leftButton.wasPressedThisFrame + "; released=" + mouse.leftButton.wasReleasedThisFrame);
        Step(End(endpoint).position + surface.right * .045f, true);
        Step(target, true);
        Check(puzzle.GetSocket(endpoint) == -1, "Hover must not snap before release.");
        Step(target, false);
    }
    public static string Run()
    {
        Check(Application.isPlaying, "Requires Play Mode.");
        board = GameObject.Find("Minigames/Wires").GetComponent<WiresBoard>();
        cables = (WiresCable[])Read(board, "cables"); sockets = (WiresSocket[])Read(board, "sockets");
        puzzle = (WiresPuzzle)Read(board, "puzzle"); camera = Camera.main; surface = (Transform)Read(board, "surface");
        Check(puzzle != null && !puzzle.IsCompleted, "Puzzle must initialize incomplete.");
        var scenery = surface.Find("Scenery");
        Transform[] sceneryTransforms = scenery == null ? new Transform[0] : scenery.GetComponentsInChildren<Transform>();
        var sceneryPositions = new Vector3[sceneryTransforms.Length];
        for (int i = 0; i < sceneryTransforms.Length; i++) sceneryPositions[i] = sceneryTransforms[i].position;
        if (scenery != null)
        {
            Check(cables.Length == 4 && sockets.Length == 8, "Scenery must not add puzzle entries or cables.");
            Check(scenery.GetComponentsInChildren<Collider>(true).Length == 0, "Scenery has no selectable colliders.");
            Check(scenery.GetComponentsInChildren<WiresSocket>(true).Length == 0 && scenery.GetComponentsInChildren<WiresCable>(true).Length == 0,
                "Scenery must not have interactive socket/cable components.");
            Check(scenery.GetComponentsInChildren<WiresCableObstacle>().Length == 2, "Two fixed cable paths.");
        }
        var controller = camera.GetComponent<CameraController>();
        var target = (Transform)Read(board, "cameraTarget");
        var overview = GameObject.Find("CameraPositions/Overview").transform;
        float duration = (float)Read(controller, "moveDuration");
        float tolerance = (float)Read(board, "snapTolerance");
        var mode = InputSystem.settings.updateMode; var previousMouse = Mouse.current;
        int completions = 0; UnityEngine.Events.UnityAction completed = () => completions++;
        board.OnCompleted.AddListener(completed);
        mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
        try
        {
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            Write(controller, "moveDuration", 0f);
            controller.MoveTo(overview);
            Check(!board.CanInteract, "Overview must block plug input.");
            foreach (var cable in cables) Check(!cable.GrabCollider(0).enabled && !cable.GrabCollider(1).enabled, "Overview disables all tip colliders.");
            controller.MoveTo(target);
            Check(board.CanInteract, "Focused target must accept input.");
            Capture("Temp/Wires-Play-Initial.png");
            if (scenery != null)
            {
                Check(Mathf.Abs(tolerance - .045f) < .000001f, "Snap tolerance increased 80 percent.");
                Drag(0, scenery.Find("SceneryPort_espiral/PlugAnchor").position);
                Check(puzzle.GetSocket(0) == -1, "Dropping a playable cable on a scenery port must not connect.");
            }
            // Both Z entries inside tolerance: first eligible entry wins, then occupied one is skipped.
            Write(board, "snapTolerance", .045f);
            Vector3 midpoint = (sockets[0].Position + sockets[1].Position) * .5f;
            Drag(0, midpoint); Check(puzzle.GetSocket(0) == 0, "Z tie chooses first free entry.");
            Drag(1, midpoint); Check(puzzle.GetSocket(1) == 1, "Z permits same cable at both entries.");
            Write(board, "snapTolerance", tolerance);
            Drag(2, sockets[0].Position);
            Check(puzzle.GetSocket(2) == -1 && puzzle.GetSocket(0) == 0, "Occupied target is ignored without replacement.");
            for (int i = 2; i < 8; i++) Drag(i, sockets[i].Position);
            Check(!board.IsCompleted && completions == 0, "Wrong complete arrangement stays editable and emits no success.");
            controller.MoveTo(overview); controller.MoveTo(target);
            for (int i = 0; i < 8; i++) Check(puzzle.GetSocket(i) == i, "Camera navigation preserves connections.");
            for (int i = 0; i < 8; i++)
                Drag(i, surface.TransformPoint(new Vector3((i % 4 - 1.5f) * .06f, i < 4 ? -.06f : .06f, -.038f)));
            int[] solution = { 7, 0, 5, 4, 1, 3, 2, 6 };
            for (int i = 0; i < 8; i++) Drag(i, sockets[solution[i]].Position);
            Check(board.IsCompleted && completions == 1 && !board.CanInteract, "Solution emits success once and locks input.");
            foreach (var cable in cables) Check(!cable.GrabCollider(0).enabled && !cable.GrabCollider(1).enabled, "Completion locks all tips.");
            Step(End(0).position, true); Step(surface.position, true); Step(surface.position, false);
            Check(puzzle.GetSocket(0) == solution[0] && completions == 1, "Completed board cannot be undone by dragging.");
            controller.MoveTo(overview); controller.MoveTo(target);
            Check(board.IsCompleted && !board.CanInteract && completions == 1, "Returning preserves completion and lock.");
            CheckCrossings();
            for (int i = 0; i < sceneryTransforms.Length; i++)
                Check(sceneryTransforms[i].position == sceneryPositions[i], "Scenery must stay fixed during the whole puzzle.");
            Capture("Temp/Wires-Play-Solved.png");
            return "PASS: real pointer ray selection, drag detach, release-only snap, Z tie and Z-Z, occupied target, wrong full arrangement, correction, camera persistence, one completion and permanent input lock.";
        }
        finally
        {
            board.OnCompleted.RemoveListener(completed); Write(controller, "moveDuration", duration); Write(board, "snapTolerance", tolerance);
            InputSystem.RemoveDevice(mouse); InputSystem.settings.updateMode = mode;
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
        }
    }
    private static void CheckCrossings()
    {
        board.RefreshCableGeometry();
        for (int i = 0; i < cables.Length; i++) for (int j = 0; j < i; j++)
        {
            var a = (Vector3[])Read(cables[i], "points"); var b = (Vector3[])Read(cables[j], "points");
            float required = cables[i].Radius + cables[j].Radius;
            for (int p = 0; p < a.Length - 1; p++) for (int q = 0; q < b.Length - 1; q++)
                Check(SegmentDistance(a[p], a[p+1], b[q], b[q+1]) >= required - .0005f,
                    "Solved cable centerlines must clear both tube radii: cables " + i + "/" + j);
        }
        var scenery = (WiresCableObstacle[])Read(board, "sceneryCables");
        if (scenery == null) return;
        foreach (var obstacle in scenery) foreach (var cable in cables)
        {
            var a = (Vector3[])Read(cable, "points"); var b = obstacle.Sample(surface);
            float required = cable.Radius + obstacle.Radius;
            for (int p = 0; p < a.Length - 1; p++) for (int q = 0; q < b.Length - 1; q++)
                Check(SegmentDistance(a[p], a[p+1], b[q], b[q+1]) >= required - .0005f, "Playable cables must clear fixed scenery cables.");
        }
    }
    private static float SegmentDistance(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        Vector3 d1 = q1-p1, d2 = q2-p2, r = p1-p2;
        float a = Vector3.Dot(d1,d1), e = Vector3.Dot(d2,d2), f = Vector3.Dot(d2,r), s, t;
        if (a < 1e-12f && e < 1e-12f) return r.magnitude;
        if (a < 1e-12f) { s = 0; t = Mathf.Clamp01(f/e); }
        else
        {
            float c = Vector3.Dot(d1,r);
            if (e < 1e-12f) { t = 0; s = Mathf.Clamp01(-c/a); }
            else
            {
                float b = Vector3.Dot(d1,d2), denominator = a*e-b*b;
                s = denominator > 1e-12f ? Mathf.Clamp01((b*f-c*e)/denominator) : 0;
                t = (b*s+f)/e;
                if (t < 0) { t = 0; s = Mathf.Clamp01(-c/a); }
                else if (t > 1) { t = 1; s = Mathf.Clamp01((b-c)/a); }
            }
        }
        return Vector3.Distance(p1+d1*s,p2+d2*t);
    }
    private static void Capture(string path)
    {
        var oldTexture = camera.targetTexture; var oldActive = RenderTexture.active;
        var render = new RenderTexture(1600, 1000, 24); var texture = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = render; camera.Render(); RenderTexture.active = render;
            texture.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        { camera.targetTexture = oldTexture; RenderTexture.active = oldActive; UnityEngine.Object.DestroyImmediate(render); UnityEngine.Object.DestroyImmediate(texture); }
    }
}
