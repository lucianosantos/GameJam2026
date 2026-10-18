using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using GameJam2026.Levers;
public static class LeversUnityChecks {
 static object Field(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static void Click(LeverHandle h,PointerEventData.InputButton button=PointerEventData.InputButton.Left){ExecuteEvents.Execute(h.gameObject,new PointerEventData(EventSystem.current){button=button},ExecuteEvents.pointerClickHandler);}
 public static string Run(){
 Check(Application.isPlaying,"Play Mode required");
 var board=UnityEngine.Object.FindFirstObjectByType<LeversBoard>();var handles=(LeverHandle[])Field(board,"levers");
 var camera=Camera.main;var controller=camera.GetComponent<CameraController>();var duration=controller.GetType().GetField("moveDuration",BindingFlags.Instance|BindingFlags.NonPublic);var old=duration.GetValue(controller);
 var target=(Transform)Field(board,"cameraTarget");var overview=GameObject.Find("CameraPositions/Overview").transform;
 var light=(Renderer)Field(board,"statusLight");var properties=new MaterialPropertyBlock();int completed=0;UnityEngine.Events.UnityAction done=()=>completed++;board.OnCompleted.AddListener(done);
 Check(light.transform.IsChildOf(GameObject.Find("ShipPanel/Painel/Luz_Alavancas").transform),"Status indicator must belong to Luz_Alavancas");
 Check(GameObject.Find("ShipPanel/Painel/Luz_Cadeado").GetComponentInChildren<Renderer>().sharedMaterial!=light.sharedMaterial,"The two indicators use independent materials");
 Check(board.GetComponentsInChildren<TextMesh>().Length==0,"Position marks must not contain numbers");
 foreach(var handle in handles){
 var pivot=(Transform)Field(handle,"movingPivot");var anchor=pivot.position;var previous=Quaternion.identity;
 for(int position=1;position<=4;position++){
 handle.ShowPosition(position);Check(Vector3.Distance(pivot.position,anchor)<.00001f,"Pivot stays fixed at all four positions");
 if(position>1)Check(Quaternion.Angle(previous,pivot.localRotation)>1f,"Each position rotates the lever");previous=pivot.localRotation;
 }
 var marks=handle.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Position_")).ToArray();Check(marks.Length==4,"Four ticks per lever");
 foreach(var mark in marks)Check(mark.GetComponent<Renderer>()!=null&&Mathf.Abs(handle.transform.InverseTransformPoint(mark.position).x)>.08f,"Ticks are offset from the shaft");
 }
 var neutral=handles.Select(h=>h.GetComponentInChildren<Renderer>().sharedMaterial).ToArray();
 try{
 duration.SetValue(controller,0f);board.RestartPuzzle();controller.MoveTo(overview);Click(handles[1]);Check(handles[1].Position==1,"Overview blocks handle input");
 light.GetPropertyBlock(properties);Check(properties.GetColor("_BaseColor")==new Color(.65f,.16f,.12f),"Pending red matches LightSequence");
 controller.MoveTo(target);Physics.SyncTransforms();
 Check(board.CanInteract,"Focused board accepts input");
 var area=GameObject.Find("ShipPanel/LeversClickArea").GetComponent<Collider>();Check(!area.enabled,"Approach collider disables in focus");
 foreach(var h in handles){var screen=camera.WorldToScreenPoint(h.transform.position+Vector3.up*.1f);var ray=camera.ScreenPointToRay(screen);Check(Physics.Raycast(ray,out var hit)&&hit.collider.GetComponent<LeverHandle>()==h,"Physical raycast selects "+h.name);}
 Click(handles[1]);Check(handles[1].Position==2&&!board.IsCompleted,"Partial correct combination stays unsolved");Click(handles[2]);Check(handles[2].Position==2&&!board.IsCompleted,"Incorrect combination stays unsolved");
 controller.MoveTo(overview);Check(area.enabled,"Overview restores approach collider");Click(handles[2]);Check(handles[2].Position==2,"Overview preserves progress");controller.MoveTo(target);
 Click(handles[2],PointerEventData.InputButton.Right);Check(handles[2].Position==1,"Right click decreases");Click(handles[2]);board.SetPosition(handles[0],4);board.SetPosition(handles[1],4);Check(board.IsCompleted&&completed==1,"Exact 4,4,2 solution completes once");
 light.GetPropertyBlock(properties);Check(properties.GetColor("_BaseColor")==new Color(.18f,.55f,.24f),"Only global indicator changes green");
 for(int i=0;i<3;i++){Click(handles[i]);Check(handles[i].Position==new[]{4,4,2}[i],"Success locks "+i);Check(handles[i].GetComponentInChildren<Renderer>().sharedMaterial==neutral[i],"No per-handle material feedback");}
 controller.MoveTo(overview);controller.MoveTo(target);Check(board.IsCompleted&&!board.CanInteract,"Success survives refocus");
 board.RestartPuzzle();Check(!board.IsCompleted&&handles.All(h=>h.Position==1),"Restart restores all handles");
 // Exercise drag events with a real PhysicsRaycaster and projected world displacement.
 var h0=handles[0];var start=camera.WorldToScreenPoint(h0.transform.position);var end=camera.WorldToScreenPoint(h0.transform.position+Vector3.ProjectOnPlane(h0.transform.forward,camera.transform.forward).normalized*.28f);
 var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=start,pointerPressRaycast=new RaycastResult{module=camera.GetComponent<PhysicsRaycaster>()}};
 h0.OnBeginDrag(data);data.position=end;h0.OnDrag(data);h0.OnEndDrag(data);Check(h0.Position==4,"Drag reaches fourth discrete position");Check(!data.eligibleForClick,"Drag suppresses subsequent click");
 return "PASS: Luz_Alavancas indicator, restored Luz_Cadeado, fixed pivots, four distinct rotations, four separated ticks without numbers, exact red/green, focus gating, physical raycasts, clicks, persistence, single completion, neutral handles, lock, restart and drag.";
 }finally{board.OnCompleted.RemoveListener(done);board.RestartPuzzle();duration.SetValue(controller,old);controller.MoveTo(overview);}
 }
 public static string View(){var board=UnityEngine.Object.FindFirstObjectByType<LeversBoard>();var controller=Camera.main.GetComponent<CameraController>();controller.GetType().GetField("moveDuration",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,0f);controller.MoveTo((Transform)Field(board,"cameraTarget"));return "Focused";}
}
