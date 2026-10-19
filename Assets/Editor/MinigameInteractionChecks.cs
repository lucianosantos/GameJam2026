using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Runs against real Unity components in Edit Mode, without changing the saved scene.
public static class MinigameInteractionChecks
{
    [MenuItem("Tools/Validation/Check Minigame Interaction")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before running this check.");
        var temporaryObjects = new List<GameObject>();
        CameraClickArea firstArea = null, secondArea = null;
        try
        {
            GameObject overview = Create("Check Overview", temporaryObjects);
            GameObject minigameTarget = Create("Check Minigame Target", temporaryObjects);
            GameObject otherTarget = Create("Check Other Target", temporaryObjects);
            var controller = Create("Check Camera", temporaryObjects).AddComponent<CameraController>();
            SetField(controller, "overview", overview.transform);
            SetField(controller, "moveDuration", 0f);
            Invoke(controller, "Start");

            GameObject first = Create("Check First Area", temporaryObjects);
            var firstCollider = first.AddComponent<BoxCollider>();
            var originallyDisabled = first.AddComponent<BoxCollider>();
            originallyDisabled.enabled = false;
            firstArea = first.AddComponent<CameraClickArea>();
            SetField(firstArea, "cameraController", controller);
            Invoke(firstArea, "Awake");
            Invoke(firstArea, "OnDisable");
            Invoke(firstArea, "OnEnable");

            GameObject second = Create("Check Second Area", temporaryObjects);
            var secondCollider = second.AddComponent<BoxCollider>();
            secondArea = second.AddComponent<CameraClickArea>();
            SetField(secondArea, "cameraController", controller);
            Invoke(secondArea, "Awake");
            Invoke(secondArea, "OnDisable");
            Invoke(secondArea, "OnEnable");

            Check(firstCollider.enabled && secondCollider.enabled, "Overview enables both approach colliders");

            bool sawTransition = false;
            controller.ViewChanged += () =>
            {
                if (controller.ViewState != CameraViewState.Transitioning) return;
                sawTransition = true;
                Check(!firstCollider.enabled && !secondCollider.enabled, "Transition disables both colliders");
            };
            controller.MoveTo(minigameTarget.transform);
            Check(sawTransition, "Zoom passes through the transition state");
            Check(controller.IsFocusedOn(minigameTarget.transform), "Minigame target is active");
            Check(!firstCollider.enabled && !secondCollider.enabled, "Zoom keeps both colliders disabled");

            controller.MoveTo(otherTarget.transform);
            controller.MoveTo(overview.transform);
            Check(firstCollider.enabled && secondCollider.enabled, "Returning restores both colliders");
            Check(!originallyDisabled.enabled, "Originally disabled colliders stay disabled");
            Debug.Log("PASS: overview, zoom transition, target focus, both approach colliders and return to overview.");
        }
        finally
        {
            if (firstArea != null) Invoke(firstArea, "OnDisable");
            if (secondArea != null) Invoke(secondArea, "OnDisable");
            for (int i = temporaryObjects.Count - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(temporaryObjects[i]);
        }
    }

    private static GameObject Create(string name, List<GameObject> objects)
    {
        var result = new GameObject(name);
        objects.Add(result);
        return result;
    }

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static void Invoke(object target, string name) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }
}
