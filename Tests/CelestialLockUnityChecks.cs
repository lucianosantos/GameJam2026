using System;
using System.Reflection;
using System.Threading.Tasks;
using GameJam2026.CelestialLock;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CelestialLockUnityChecks
{
    private static object Read(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void CheckLight(CelestialLockBoard board, bool completed)
    {
        var light = (Renderer)Read(board,"statusLight");
        Check(light != null && light.transform.IsChildOf(GameObject.Find("ShipPanel/Painel/Luz_Cadeado").transform), "Status renderer belongs to Luz_Cadeado");
        var block = new MaterialPropertyBlock(); light.GetPropertyBlock(block);
        Color expected = (Color)Read(board,completed ? "completedColor" : "pendingColor");
        Color actual = block.GetColor("_BaseColor");
        Check(Vector4.Distance(actual,expected)<.0001f,"Luz_Cadeado has expected " + (completed ? "green" : "red") + " color");
        Check(Vector4.Distance(block.GetColor("_EmissionColor"),expected*(float)Read(board,"emissionIntensity"))<.0001f,"Status emission follows color");
        Check(Vector4.Distance(light.sharedMaterial.GetColor("_BaseColor"),(Color)Read(board,"pendingColor"))<.0001f,"Shared material stays authored red");
    }
    private static void Step(CelestialLockBoard board, Mouse mouse, Vector3 world, bool down)
    {
        var screen = (Vector2)Camera.main.WorldToScreenPoint(world);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = down ? (ushort)1 : (ushort)0 });
        InputSystem.Update();
        typeof(CelestialLockBoard).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(board, null);
    }
    private static void Click(CelestialLockBoard board, Mouse mouse, Collider collider)
    {
        Vector3 world = collider.bounds.center;
        Physics.SyncTransforms();
        var ray = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(world));
        Check(Physics.Raycast(ray, out RaycastHit hit) && hit.collider == collider, "Collider must be reachable by camera ray: " + collider.name);
        Step(board, mouse, world, false); Step(board, mouse, world, true); Step(board, mouse, world, false);
    }
    public static async Task<string> Run()
    {
        Check(Application.isPlaying, "Requires Play Mode");
        var board = GameObject.Find("Minigames/CelestialLock").GetComponent<CelestialLockBoard>();
        var controller = Camera.main.GetComponent<CameraController>();
        var overview = (Transform)Read(controller,"overview");
        var hits = (Collider[])Read(board,"rollerColliders"); var confirm = (Collider)Read(board,"confirmCollider");
        var originalMode = InputSystem.settings.updateMode; var oldMouse = Mouse.current;
        var mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
        float duration = (float)Read(controller,"moveDuration");
        controller.GetType().GetField("moveDuration",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,0f);
        int completions = 0; UnityEngine.Events.UnityAction complete = () => completions++;
        board.OnCompleted.AddListener(complete);
        try
        {
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            board.RestartPuzzle(); controller.MoveTo(overview);
            CheckLight(board,false);
            Check(!board.Advance(0) && !board.Submit(), "Overview blocks all gameplay");
            var area = GameObject.Find("ShipPanel/CelestialLockClickArea").transform;
            Check(area.GetComponent<Collider>().enabled,"Overview area is enabled");
            // Use the actual overview click handler to enter the new minigame.
            Vector3 center = area.GetComponent<Collider>().bounds.center;
            Step(board,mouse,center,false);
            Step(board,mouse,center,true);
            typeof(CameraClickArea).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(area.GetComponent<CameraClickArea>(),null);
            Step(board,mouse,center,false);
            Check(board.CanInteract && !area.GetComponent<Collider>().enabled,"Overview click focuses lock and releases gameplay raycasts");
            Click(board,mouse,confirm);
            Check(!board.IsCompleted && completions==0,"Wrong confirmation stays editable");
            CheckLight(board,false);
            for(int i=0;i<4;i++) Check(board.SymbolAt(i)==3-i,"Wrong confirm preserves order");
            // A script compile can leave a long unscaledDeltaTime on this frame.
            // Hold this first turn long enough to check the animation gate deterministically.
            board.GetType().GetField("turnDuration",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(board,1000f);
            Click(board,mouse,hits[0]);
            Check(board.SymbolAt(0)==0 && !board.Advance(0),"One real click advances Saturn to Moon, no second turn mid-animation");
            Check(!board.Submit(),"Cannot confirm during a turn");
            board.GetType().GetField("turnDuration",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(board,.18f);
            controller.MoveTo(overview); controller.MoveTo(board.CameraTarget);
            Check(board.SymbolAt(0)==0,"Leaving and returning preserves selected symbols");
            await Task.Delay(260);
            var solution = (CelestialSymbol[])Read(board,"correctOrder");
            for(int i=0;i<4;i++)
                while(board.SymbolAt(i)!=(int)solution[i]) { Click(board,mouse,hits[i]); await Task.Delay(260); }
            Check(!board.IsCompleted,"Correct alignment alone never completes");
            Click(board,mouse,confirm);
            Check(board.IsCompleted && completions==1,"Real CONFIRM click completes exactly once");
            CheckLight(board,true);
            controller.MoveTo(overview); controller.MoveTo(board.CameraTarget);
            Check(!board.Advance(0) && !board.Submit() && completions==1,"Completion remains locked across views");
            board.gameObject.SetActive(false); board.gameObject.SetActive(true);
            Check(board.IsCompleted && !board.CanInteract,"Disable/enable preserves completion");
            CheckLight(board,true);
            board.RestartPuzzle(); Check(!board.IsCompleted && board.SymbolAt(0)==3,"Explicit restart restores original order");
            CheckLight(board,false);
            var session=UnityEngine.Object.FindFirstObjectByType<GameJam2026.GameSession>();
            if(session!=null) Check((CelestialLockBoard)Read(session,"celestialLock")==board,"GameSession registers the celestial lock");
            return "PASS: real clicks, overview entry, animation gate, confirmation, persistence, unique completion, GameSession registration and Luz_Cadeado red/green/emission/restart without mutating its shared material.";
        }
        finally
        {
            board.OnCompleted.RemoveListener(complete); board.RestartPuzzle();
            board.GetType().GetField("turnDuration",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(board,.18f);
            controller.MoveTo(overview);
            controller.GetType().GetField("moveDuration",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,duration);
            InputSystem.settings.updateMode=originalMode; InputSystem.RemoveDevice(mouse); if(oldMouse!=null)oldMouse.MakeCurrent();
        }
    }
}
