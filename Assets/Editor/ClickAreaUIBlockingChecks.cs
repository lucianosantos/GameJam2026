using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ClickAreaUIBlockingChecks
{
    [MenuItem("Tools/Validation/Check Click Area UI Blocking")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run in Edit Mode.");
        var previousEvents = EventSystem.current;
        var root = new GameObject("Temporary click navigation check");
        CameraClickArea area = null;
        try
        {
            var events = Child(root, "Events").AddComponent<EventSystem>();
            EventSystem.current = events;
            var cameraObject = Child(root, "Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<PhysicsRaycaster>();
            var overview = Child(root, "Overview").transform;
            overview.position = new Vector3(10000, 10000, -10);
            var target = Child(root, "Target").transform;
            target.position = overview.position + Vector3.forward;
            var controller = cameraObject.AddComponent<CameraController>();
            cameraObject.GetComponent<PhysicsRaycaster>().enabled = false;
            cameraObject.AddComponent<GameJam2026.GamePointerRaycaster>();
            Set(controller, "overview", overview);
            Set(controller, "moveDuration", 0f);
            Invoke(controller, "Start");
            var areaObject = Child(root, "Area");
            areaObject.transform.position = new Vector3(10000, 10000, 0);
            areaObject.AddComponent<BoxCollider>();
            area = areaObject.AddComponent<CameraClickArea>();
            Set(area, "targetPosition", target);
            Set(area, "cameraController", controller);
            Invoke(area, "Awake");
            Invoke(area, "OnDisable");
            Invoke(area, "OnEnable");
            var obstruction = Child(root, "Foreground scenery");
            obstruction.transform.position = areaObject.transform.position + Vector3.back * 2;
            obstruction.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Vector2 point = camera.WorldToScreenPoint(areaObject.transform.position);
            var hits = new List<RaycastResult>();
            events.RaycastAll(new PointerEventData(events) { position = point }, hits);
            Check(hits.Exists(hit => hit.module is PhysicsRaycaster && hit.gameObject == areaObject),
                "EventSystem sees the physical approach collider.");
            Check(!CameraClickArea.IsBlockedByScreenUI(point), "PhysicsRaycaster hit must not block navigation.");
            foreach (int pointerId in new[] { -1, 42 })
            {
                ExecuteEvents.Execute(areaObject, new PointerEventData(events)
                    { position = point, pointerId = pointerId, button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                Check(controller.IsFocusedOn(target), "Mouse/touch pointer reaches the target despite foreground scenery.");
                controller.ReturnToOverview();
                Physics.SyncTransforms();
            }

            var canvasObject = new GameObject("Screen UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var blocker = new GameObject("Blocking button", typeof(RectTransform), typeof(Image));
            var rect = blocker.GetComponent<RectTransform>();
            rect.SetParent(canvasObject.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            Check(CameraClickArea.IsBlockedByScreenUI(point), "Screen UI still blocks world navigation.");
            Check(!area.TryNavigateAtScreenPoint(camera, point) && controller.ViewState == CameraViewState.Overview,
                "Clicking an overlay cannot move the camera behind it.");
            hits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = point }, hits);
            Check(hits.Count > 0 && hits[0].gameObject == blocker, "Overlay wins EventSystem sorting over the game raycaster.");
            Debug.Log("PASS: real PhysicsRaycaster result allows navigation; overlay UI blocks navigation.");
        }
        finally
        {
            if (area != null) Invoke(area, "OnDisable");
            UnityEngine.Object.DestroyImmediate(root);
            EventSystem.current = previousEvents;
        }
    }

    private static GameObject Child(GameObject root, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        return child;
    }
    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Invoke(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
