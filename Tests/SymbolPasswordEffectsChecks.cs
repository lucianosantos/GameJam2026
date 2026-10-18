using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameJam2026.SymbolPassword;
public static class SymbolPasswordEffectsChecks {
 static FieldInfo F(object o,string n)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance);
 static MethodInfo M(object o,string n)=>o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance);
 static void Check(bool v,string m){if(!v)throw new Exception(m);}
 public static string Run(){
 var board=UnityEngine.Object.FindFirstObjectByType<SymbolPasswordBoard>();var keys=board.GetComponentsInChildren<SymbolPasswordButton>();
 Check(board.CanInteract,"Focus required");
 var key=keys.First(k=>k.Action==PasswordKeyAction.Digit);var visual=(Transform)F(key,"visual").GetValue(key);var rest=visual.localPosition;
 var ev=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerId=-1};
 var legends=(Transform[])F(key,"legends").GetValue(key);var legendPositions=legends.Select(t=>t.localPosition).ToArray();
 key.OnPointerDown(ev);F(key,"amount").SetValue(key,.5f);M(key,"Update").Invoke(key,null);Check(visual.localPosition.z>rest.z,"Press travels into panel");
 for(int i=0;i<legends.Length;i++)Check(Vector3.Distance(legends[i].localPosition-legendPositions[i],visual.localPosition-rest)<.00001f,"Legend follows key press");
 key.OnPointerExit(ev);Check(!(bool)F(key,"held").GetValue(key),"Exit releases hold");
 key.enabled=false;Check(visual.localPosition==rest,"Disable restores visual");for(int i=0;i<legends.Length;i++)Check(legends[i].localPosition==legendPositions[i],"Disable restores legend");key.enabled=true;
 foreach(var k in keys)M(k,"Update").Invoke(k,null);
 Physics.SyncTransforms();
 foreach(var k in keys){var point=Camera.main.WorldToScreenPoint(k.transform.position);RaycastHit hit;Check(Physics.Raycast(Camera.main.ScreenPointToRay(point),out hit),"Key ray hits");Check(hit.collider.GetComponent<SymbolPasswordButton>()==k,"Key surface reachable: "+k.name+" actual "+hit.collider.name);}
 var progress=board.Progress;var flash=(IEnumerator)M(board,"FlashError").Invoke(board,null);int yields=0;
 while(flash.MoveNext()){
 bool red=yields%2==0;var monitor=(Image)F(board,"monitor").GetValue(board);Check((monitor.color.r>.9f&&monitor.color.g<.1f)==red,"Monitor alternates");
 foreach(var k in keys){var p=new MaterialPropertyBlock();k.GetComponentInChildren<Renderer>().GetPropertyBlock(p);var c=p.GetColor("_BaseColor");Check((c.r>.9f&&c.g<.1f)==red,"Keys alternate");}
 Check(flash.Current is WaitForSecondsRealtime,"Unscaled timed flashes");yields++;}
 Check(yields==5&&board.Progress==progress,"Three flashes preserve input");
 return "PASS: press travel, pointer exit, disable restoration, physical raycast reachability for all 12 keys, three red/off flashes on monitor and every key, unscaled waits and input preservation.";
 }
}
