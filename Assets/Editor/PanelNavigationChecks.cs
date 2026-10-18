using System;
using System.Linq;
using System.Reflection;
using GameJam2026.Levers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PanelNavigationChecks
{
    [MenuItem("Tools/Validation/Check Panel Navigation In Play Mode")]
    public static void Run()
    {
        Check(Application.isPlaying, "Play Mode required.");
        var camera = Camera.main;
        var controller = camera.GetComponent<CameraController>();
        var navigation = camera.GetComponent<ShipPanelNavigation>();
        var back = (GameObject)Read(navigation, "backButton");
        var overview = GameObject.Find("CameraPositions/Overview").transform;
        float duration = (float)Read(controller, "moveDuration");
        typeof(CameraController).GetField("moveDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, 0f);
        try
        {
            controller.ReturnToOverview();
            Refresh(navigation);
            Check(controller.ViewState == CameraViewState.Overview && !back.activeSelf, "Overview hides back button.");
            var areas = UnityEngine.Object.FindObjectsByType<CameraClickArea>(FindObjectsSortMode.None);
            Physics.SyncTransforms();
            foreach (var area in areas)
            {
                Check(area.IsAvailable && area.TryGetBounds(out _), "Overview enables area: " + area.name);
            }
            foreach (var target in areas.Select(a => a.TargetPosition).Distinct())
            {
                // Some authored boxes overlap or are partially behind adjacent controls.
                // Check real visible access to every destination rather than assuming each box center is exposed.
                bool reachable = false;
                foreach (var area in areas.Where(a => a.TargetPosition == target))
                {
                    area.TryGetBounds(out var bounds);
                    for (int x = 0; x < 5 && !reachable; x++)
                    for (int y = 0; y < 5 && !reachable; y++)
                    for (int z = 0; z < 3 && !reachable; z++)
                    {
                        var point = bounds.center + Vector3.Scale(bounds.extents,
                            new Vector3((x - 2) * .45f, (y - 2) * .45f, (z - 1) * .9f));
                        var screen = camera.WorldToScreenPoint(point);
                        if (screen.z <= 0 || screen.x < 0 || screen.y < 0 || screen.x > camera.pixelWidth || screen.y > camera.pixelHeight) continue;
                        var selected = CameraClickArea.FindAtScreenPoint(camera, screen);
                        if (selected != null && selected.TargetPosition == target && !CameraClickArea.IsBlockedByScreenUI(screen))
                        {
                            Check(selected.TryNavigateAtScreenPoint(camera, screen), "Click handler accepts " + target.name);
                            Check(controller.IsFocusedOn(target), "Click handler focuses " + target.name);
                            controller.ReturnToOverview();
                            Physics.SyncTransforms();
                            reachable = true;
                        }
                    }
                }
                Check(reachable, "Visible overview ray reaches " + target.name);
                controller.MoveTo(target);
                Refresh(navigation);
                Check(back.activeSelf && controller.IsFocusedOn(target), "Back button available at " + target.name);
                Check(areas.All(a => !a.IsAvailable && !a.GetComponent<Collider>().enabled), "Focus releases gameplay colliders.");
                back.GetComponent<Button>().onClick.Invoke();
                Refresh(navigation);
                Check(controller.ActiveTarget == overview && !back.activeSelf, "Button returns to overview from " + target.name);
            }
            var levers = UnityEngine.Object.FindFirstObjectByType<LeversBoard>();
            var handles = (LeverHandle[])Read(levers, "levers");
            var solution = (int[])Read(levers, "correctPositions");
            var leverTarget = (Transform)Read(levers, "cameraTarget");
            controller.MoveTo(leverTarget);
            for (int i = 0; i < handles.Length; i++) levers.SetPosition(handles[i], solution[i]);
            Check(levers.IsCompleted && controller.IsFocusedOn(leverTarget), "Completion preserves focused camera.");
            Refresh(navigation);
            Check(back.activeSelf, "Completed game still offers Back to panel.");
            back.GetComponent<Button>().onClick.Invoke();
            controller.MoveTo(leverTarget);
            Check(levers.IsCompleted, "Returning and refocusing preserves completion.");
            levers.RestartPuzzle();
            Debug.Log("PASS: physical approach rays, all focus targets, back button, collider gating, manual return after completion and progress persistence.");
        }
        finally
        {
            controller.ReturnToOverview();
            typeof(CameraController).GetField("moveDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, duration);
            Refresh(navigation);
        }
    }

    private static object Read(object target, string name) => target.GetType().GetField(name,
        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Refresh(ShipPanelNavigation navigation) => typeof(ShipPanelNavigation).GetMethod("Refresh",
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(navigation, null);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
