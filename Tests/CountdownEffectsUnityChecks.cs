using System;
using System.Reflection;
using GameJam2026;
using UnityEngine;
using UnityEngine.UI;

// Run CountdownEffectsUnityChecks.Run in Unity (Edit or Play Mode).
// Uses temporary objects only; never changes the live camera, planet or game session.
public static class CountdownEffectsUnityChecks
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

    public static string Run()
    {
        var root = new GameObject("Countdown verification", typeof(RectTransform), typeof(Canvas));
        root.hideFlags = HideFlags.HideAndDontSave;
        var sessionObject = new GameObject("Temporary session", typeof(CameraController), typeof(GameSession));
        sessionObject.hideFlags = HideFlags.HideAndDontSave;
        var planet = new GameObject("Temporary planet", typeof(SphereCollider));
        planet.hideFlags = HideFlags.HideAndDontSave;
        var impact = new GameObject("Temporary impact point");
        impact.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var digitsObject = new GameObject("Digits", typeof(RectTransform), typeof(SevenSegmentDisplay));
            digitsObject.transform.SetParent(root.transform, false);
            digitsObject.GetComponent<RectTransform>().sizeDelta = new Vector2(264, 80);
            var digits = digitsObject.GetComponent<SevenSegmentDisplay>();
            digits.color = Color.red;
            digits.SetSeconds(600);
            digits.Rebuild(CanvasUpdate.PreRender);
            Mesh first = digits.canvasRenderer.GetMesh();
            Check(first != null && first.vertexCount == 180, "10:00 generates all LED segments through Graphic.Rebuild");
            Color32[] before = first.colors32;
            digits.SetSeconds(599);
            digits.Rebuild(CanvasUpdate.PreRender);
            Color32[] after = digits.canvasRenderer.GetMesh().colors32;
            bool changed = false;
            for (int i = 0; i < before.Length; i++) if (!before[i].Equals(after[i])) changed = true;
            Check(changed, "09:59 changes the visible segment colors");

            var borderObject = new GameObject("Border", typeof(RectTransform), typeof(ScreenWarningOverlay));
            borderObject.transform.SetParent(root.transform, false);
            borderObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            var border = borderObject.GetComponent<ScreenWarningOverlay>();
            border.color = new Color(1, 0, 0, .48f);
            border.Rebuild(CanvasUpdate.PreRender);
            Mesh borderMesh = border.canvasRenderer.GetMesh();
            Check(borderMesh != null && borderMesh.vertexCount > 16, "Smooth vignette renders");
            Color32[] borderColors = borderMesh.colors32;
            Vector3[] borderVertices = borderMesh.vertices;
            bool clearCenter = false, softEdge = false;
            int distinctAlphas = 0;
            var alphas = new bool[256];
            for (int i = 0; i < borderColors.Length; i++)
            {
                byte alpha = borderColors[i].a;
                if (!alphas[alpha]) { alphas[alpha] = true; distinctAlphas++; }
                Check(alpha < .48f * 255f, "Full-intensity outer rim remains outside the screen");
                if (borderVertices[i].sqrMagnitude < 1f && alpha == 0) clearCenter = true;
                if (Mathf.Abs(borderVertices[i].y - 540f) < .1f && Mathf.Abs(borderVertices[i].x) < 1f && alpha > 0)
                    softEdge = true;
            }
            Check(clearCenter && softEdge && distinctAlphas > 32, "Broad smooth falloff leaves the center transparent");

            var session = sessionObject.GetComponent<GameSession>();
            var controller = sessionObject.GetComponent<CameraController>();
            Type type = typeof(GameSession);
            Field(type, "controller").SetValue(session, controller);
            Field(type, "planet").SetValue(session, planet.transform);
            Field(type, "planetImpactPoint").SetValue(session, impact.transform);
            Vector3 initialPosition = new Vector3(1.5f, 1f, 2f);
            planet.transform.position = initialPosition;
            type.GetMethod("InitializePlanetApproach", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, null);
            Field(type, "warningOverlay").SetValue(session, border);
            MethodInfo effects = type.GetMethod("UpdateTimeEffects", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (double remaining in new[] { 600d, 300d, 240d, 235d, 60d, 59.4d, 0d })
            {
                var clock = new CountdownClock(600);
                clock.Advance(600 - remaining);
                Field(type, "clock").SetValue(session, clock);
                effects.Invoke(session, null);
                float progress = (float)(1 - remaining / 600);
                float expected = Mathf.Lerp(1, 9, progress);
                Check(Mathf.Abs(planet.transform.localScale.x - expected) < .0001f, "Planet follows elapsed time");
                float clearance = Vector3.Distance(planet.transform.position, impact.transform.position) - expected * .5f;
                float expectedClearance = Mathf.Lerp(initialPosition.magnitude - .5f, .5f, progress);
                Check(Mathf.Abs(clearance - expectedClearance) < .0001f, "Surface approaches impact point without crossing it early");
                if (remaining == 600) Check(Vector3.Distance(planet.transform.position, initialPosition) < .0001f,
                    "Approach preserves the authored initial position");
                float shake = (float)Field(typeof(CameraController), "timeShake").GetValue(controller);
                Check(remaining >= 240 ? shake == 0 : shake > 0, "Shake starts smoothly at four minutes");
                Check(remaining > 60 ? border.color.a == 0 : border.color.a > 0, "Warning starts at one minute");
                if (remaining == 60) Check(Mathf.Abs(border.color.a - .48f) < .001f, "Siren starts at visible peak");
                if (remaining == 59.4d) Check(Mathf.Abs(border.color.a - .06f) < .001f, "Siren fades after half a cycle");
            }
            controller.EndGame();
            Check(controller.IsGameOver && (float)Field(typeof(CameraController), "timeShake").GetValue(controller) == 0,
                "Ending the game stops shake");
            return "PASS: LED mesh, soft elliptical vignette, 4x growth increment, final surface clearance, shake, siren and stop.";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(sessionObject);
            UnityEngine.Object.DestroyImmediate(planet);
            UnityEngine.Object.DestroyImmediate(impact);
        }
    }
}
