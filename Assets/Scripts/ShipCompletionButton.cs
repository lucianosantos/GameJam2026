using UnityEngine;
using UnityEngine.InputSystem;

namespace GameJam2026
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider), typeof(Renderer))]
    public sealed class ShipCompletionButton : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private CameraController controller;
        [SerializeField] private Transform monitorView;
        [SerializeField] private Color readyBlue = new Color(.12f, .28f, 1f);
        [SerializeField, Min(.1f)] private float pulseSeconds = 2f;
        private Renderer lens;
        private Material material;
        private Color originalColor;
        private Texture originalTexture;
        private Color originalEmission;
        private bool originalEmissionEnabled;
        private PuzzleStatusGlow glow;
        private bool wasReady;
        private float readyAt;

        public bool CanPress => isActiveAndEnabled && session != null && session.IsReadyToFinish &&
            controller != null && !controller.IsGameOver &&
            (controller.ViewState == CameraViewState.Overview || controller.IsFocusedOn(monitorView));

        private void Awake()
        {
            lens = GetComponent<Renderer>();
            material = lens.material;
            originalColor = material.GetColor("_BaseColor");
            originalTexture = material.GetTexture("_BaseMap");
            originalEmission = material.GetColor("_EmissionColor");
            originalEmissionEnabled = material.IsKeywordEnabled("_EMISSION");
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                TryPressAtScreenPoint(Camera.main, Mouse.current.position.ReadValue());
        }

        private void LateUpdate()
        {
            bool ready = session != null && session.IsReadyToFinish && !session.HasEnded;
            if (ready)
            {
                if (!wasReady) readyAt = Time.unscaledTime;
                // Always lit: a smooth breath, never an on/off blink.
                float pulse = .5f - .5f * Mathf.Cos((Time.unscaledTime - readyAt) * Mathf.PI * 2f / pulseSeconds);
                float strength = Mathf.Lerp(.65f, 1.5f, pulse);
                material.SetColor("_BaseColor", readyBlue);
                material.SetTexture("_BaseMap", null);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", readyBlue * strength);
                PuzzleStatusGlow.Apply(lens, readyBlue * strength);
                if (glow == null) glow = GetComponent<PuzzleStatusGlow>();
                glow.SetVisible(true);
                glow.SetLightIntensity(strength);
            }
            else if (wasReady)
            {
                material.SetColor("_BaseColor", originalColor);
                material.SetTexture("_BaseMap", originalTexture);
                material.SetColor("_EmissionColor", originalEmission);
                if (!originalEmissionEnabled) material.DisableKeyword("_EMISSION");
                if (glow != null) glow.SetVisible(false);
            }
            wasReady = ready;
        }

        public bool TryPressAtScreenPoint(Camera camera, Vector2 point)
        {
            if (!CanPress || CameraClickArea.IsBlockedByScreenUI(point) || FindAtScreenPoint(camera, point) != this)
                return false;
            return session.TryFinishGame();
        }

        public static ShipCompletionButton FindAtScreenPoint(Camera camera, Vector2 point)
        {
            if (camera == null) return null;
            var hits = Physics.RaycastAll(camera.ScreenPointToRay(point), Mathf.Infinity,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Collider closest = null;
            float distance = float.PositiveInfinity;
            foreach (var hit in hits)
            {
                // Approach boxes are navigation targets, not physical obstructions.
                if (hit.collider.GetComponent<CameraClickArea>() != null || hit.distance >= distance) continue;
                closest = hit.collider;
                distance = hit.distance;
            }
            if (closest == null) return null;
            var button = closest.GetComponent<ShipCompletionButton>();
            return button != null && button.CanPress ? button : null;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
