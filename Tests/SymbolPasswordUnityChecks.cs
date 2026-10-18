using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameJam2026.SymbolPassword;
public static class SymbolPasswordUnityChecks {
 static FieldInfo F(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static SymbolPasswordBoard Board=>UnityEngine.Object.FindFirstObjectByType<SymbolPasswordBoard>();
 static void Click(SymbolPasswordButton b)=>ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerId=-1},ExecuteEvents.pointerClickHandler);
 static SymbolPasswordButton Key(SymbolPasswordButton[] keys,int d)=>keys.Single(k=>k.Action==PasswordKeyAction.Digit&&k.Digit==d);
 public static string Run(){
 Check(Application.isPlaying,"Play Mode required"); var board=Board; var keys=board.GetComponentsInChildren<SymbolPasswordButton>();
 Check(keys.Length==12,"12 keys"); Check(keys.Count(k=>k.Action==PasswordKeyAction.Digit)==10,"10 digits");
 for(int i=0;i<10;i++) Check(Key(keys,i)!=null,"Unique digit "+i);
 var clear=keys.Single(k=>k.Action==PasswordKeyAction.Clear); var ok=keys.Single(k=>k.Action==PasswordKeyAction.Submit);
 var camera=Camera.main.GetComponent<CameraController>(); var duration=F(camera,"moveDuration"); float original=(float)duration.GetValue(camera);
 var target=GameObject.Find("CameraPositions/SymbolPassword").transform; var overview=GameObject.Find("CameraPositions/Overview").transform;
 int completions=0; UnityEngine.Events.UnityAction done=()=>completions++; board.OnCompleted.AddListener(done);
 try {duration.SetValue(camera,0f); board.RestartPuzzle(); camera.MoveTo(overview); Click(Key(keys,4)); Check(board.Progress==0,"Overview blocks");
 camera.MoveTo(target); Click(Key(keys,4)); Click(Key(keys,1)); Check(board.Progress==2&&!board.IsCompleted,"Input without auto validation");
 camera.MoveTo(overview); Click(clear); Check(board.Progress==2,"Overview clear blocked and attempt preserved"); camera.MoveTo(target); Check(board.Progress==2,"Refocus preserves");
 Click(clear); Check(board.Progress==0,"Clear"); foreach(int d in new[]{4,1,0,3,5})Click(Key(keys,d)); Click(Key(keys,6)); Check(board.Progress==5,"Capacity");
 Click(ok); Check(board.Progress==5&&!board.IsCompleted&&!board.CanInteract,"Wrong password preserved and blocked during flash");
 var monitor=(Image)F(board,"monitor").GetValue(board); Check(monitor.color.r>.9f&&monitor.color.g<.1f,"Monitor flashes red");
 foreach(var key in keys){var p=new MaterialPropertyBlock();key.GetComponentInChildren<Renderer>().GetPropertyBlock(p);Check(p.GetColor("_BaseColor").r>.9f&&p.GetColor("_BaseColor").g<.1f,"Every key flashes red");}
 board.enabled=false;board.enabled=true;Check(board.Progress==5&&board.CanInteract,"Disable cancels flash and preserves attempt");
 Click(clear);foreach(int d in new[]{4,1,0,3,6})Click(Key(keys,d));Check(!board.IsCompleted,"Full solution waits for Ok");
 var slots=(Image[])F(board,"displaySlots").GetValue(board); var symbols=(Sprite[])F(board,"digitSymbols").GetValue(board);
 int[] solution={4,1,0,3,6};for(int i=0;i<5;i++)Check(slots[i].enabled&&slots[i].sprite==symbols[solution[i]],"Displayed symbol matches input");
 Click(ok);Click(ok);Click(clear);Click(Key(keys,9));Check(board.IsCompleted&&completions==1&&board.Progress==5,"Completion fires once and locks");
 board.RestartPuzzle();Check(!board.IsCompleted&&board.Progress==0&&slots.All(s=>!s.enabled),"Restart");
 // Leave a representative input for visual inspection.
 foreach(int d in new[]{4,1,0})Click(Key(keys,d));
 return "PASS: 12 keys, unique 0-9, focus gate/persistence, clear, capacity, explicit Ok, error retention, red monitor/keys, disable recovery, display symbols, single completion, success lock and restart.";
 }finally{duration.SetValue(camera,original);board.OnCompleted.RemoveListener(done);}
 }
}
