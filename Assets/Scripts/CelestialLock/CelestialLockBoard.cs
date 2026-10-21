using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameJam2026.CelestialLock
{
    [DisallowMultipleComponent]
    public sealed class CelestialLockBoard : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private Transform[] rollers;
        [SerializeField] private Collider[] rollerColliders;
        [SerializeField] private Collider confirmCollider;
        [SerializeField] private Transform confirmVisual;
        [SerializeField] private Renderer confirmRenderer;
        [SerializeField] private Renderer statusLight;
        [SerializeField] private Color pendingColor = new Color(1f, .055f, .015f);
        [SerializeField] private Color completedColor = new Color(.035f, 1f, .19f);
        [SerializeField, Min(0)] private float emissionIntensity = 2.4f;
        [SerializeField] private Text[] cipherLines;
        [SerializeField, Tooltip("SUN, MOON, SATURN, STAR encoded with the manual's paired-letter diagram. Q² means QQ.")]
        private string[] cipherWords = { "USL", "HQ²L", "UCVSZL", "UVCZ" };
        [SerializeField, Tooltip("Moon=0, Star=1, Sun=2, Saturn=3.")]
        private CelestialSymbol[] correctOrder = { CelestialSymbol.Sun, CelestialSymbol.Moon, CelestialSymbol.Saturn, CelestialSymbol.Star };
        [SerializeField] private CelestialSymbol[] initialOrder = { CelestialSymbol.Saturn, CelestialSymbol.Sun, CelestialSymbol.Star, CelestialSymbol.Moon };
        [SerializeField, Min(.01f)] private float turnDuration = .18f;
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();
        private CelestialLockPuzzle puzzle;
        private readonly float[] angles = new float[4];
        private readonly float[] starts = new float[4];
        private readonly float[] elapsed = new float[4];
        private readonly bool[] turning = new bool[4];
        private Vector3 confirmRest;
        private float pressRemaining, errorRemaining;
        private MaterialPropertyBlock properties;
        private MaterialPropertyBlock statusProperties;

        public bool IsCompleted => puzzle != null && puzzle.IsCompleted;
        public UnityEvent OnCompleted => onCompleted;
        public Transform CameraTarget => cameraTarget;
        public bool IsTurning => Array.Exists(turning, value => value);
        public int SymbolAt(int index) => puzzle.SymbolAt(index);
        public bool CanInteract => isActiveAndEnabled && puzzle != null && !IsCompleted &&
            cameraController != null && cameraController.IsFocusedOn(cameraTarget);

        private void Awake()
        {
            if (confirmVisual != null) confirmRest = confirmVisual.localPosition;
            RestartPuzzle();
        }
        public void RestartPuzzle()
        {
            puzzle = null;
            RefreshStatusLight();
            if (rollers == null || rollers.Length != 4 || rollerColliders == null || rollerColliders.Length != 4 ||
                cipherLines == null || cipherLines.Length != 4 || cipherWords == null || cipherWords.Length != 4 ||
                correctOrder == null || correctOrder.Length != 4 || initialOrder == null || initialOrder.Length != 4 ||
                confirmVisual == null || confirmCollider == null || confirmRenderer == null || interactionCamera == null ||
                cameraTarget == null || cameraController == null)
            { Debug.LogError("Celestial Lock: configure four rollers, colliders, words, lines, solution, initial symbols and camera/button references.", this); return; }
            for (int i = 0; i < 4; i++)
                if (rollers[i] == null || rollerColliders[i] == null || cipherLines[i] == null ||
                    string.IsNullOrWhiteSpace(cipherWords[i]) || cipherWords[i].IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
                { Debug.LogError("Celestial Lock: each cipher line must contain one nonempty word and all roller/label references are required.", this); return; }
            try { puzzle = new CelestialLockPuzzle(Array.ConvertAll(correctOrder, s => (int)s), Array.ConvertAll(initialOrder, s => (int)s)); }
            catch (ArgumentException e) { Debug.LogError("Celestial Lock: " + e.Message, this); return; }
            for (int i = 0; i < 4; i++)
            {
                turning[i] = false; angles[i] = -90f * puzzle.SymbolAt(i);
                rollers[i].localRotation = Quaternion.Euler(angles[i], 0, 0);
                cipherLines[i].text = cipherWords[i];
            }
            pressRemaining = errorRemaining = 0;
            confirmVisual.localPosition = confirmRest;
            RefreshButton();
        }
        public bool Advance(int index)
        {
            if (!CanInteract || index < 0 || index >= 4 || turning[index] || !puzzle.Advance(index)) return false;
            starts[index] = angles[index]; elapsed[index] = 0; turning[index] = true;
            return true;
        }
        public bool Submit()
        {
            if (!CanInteract || IsTurning) return false;
            pressRemaining = .16f;
            if (puzzle.Submit())
            {
                errorRemaining = 0; RefreshButton(); RefreshStatusLight();
                SoundManager.Play(GameSound.PuzzleCompleted);
                onCompleted.Invoke(); // Completed is already set, so reentrant input is blocked.
                return true;
            }
            errorRemaining = .35f; RefreshButton();
            return false;
        }
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < 4; i++) if (turning[i])
            {
                elapsed[i] += dt;
                float t = Mathf.Clamp01(elapsed[i] / turnDuration);
                angles[i] = starts[i] - 90f * Mathf.SmoothStep(0, 1, t);
                rollers[i].localRotation = Quaternion.Euler(angles[i], 0, 0);
                if (t >= 1) { turning[i] = false; angles[i] = -90f * puzzle.SymbolAt(i); rollers[i].localRotation = Quaternion.Euler(angles[i], 0, 0); }
            }
            pressRemaining = Mathf.Max(0, pressRemaining - dt);
            if (confirmVisual != null) confirmVisual.localPosition = confirmRest + Vector3.forward * (pressRemaining > 0 ? .006f : 0);
            if (errorRemaining > 0) { errorRemaining = Mathf.Max(0, errorRemaining - dt); if (errorRemaining == 0) RefreshButton(); }
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (!CanInteract || data.button != PointerEventData.InputButton.Left) return;
            // Rollers share this ancestor handler; releasing on a different roller is not a click.
            var pressed = data.pointerPressRaycast.gameObject;
            if (pressed == null || pressed != data.pointerCurrentRaycast.gameObject) return;
            var collider = pressed.GetComponent<Collider>();
            if (collider == confirmCollider) { Submit(); return; }
            int index = Array.IndexOf(rollerColliders, collider);
            if (index >= 0) Advance(index);
        }
        private void RefreshButton()
        {
            if (confirmRenderer == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            Color color = IsCompleted ? new Color(.2f, .65f, .3f) : errorRemaining > 0 ? new Color(.75f, .15f, .08f) : new Color(.58f, .61f, .6f);
            confirmRenderer.GetPropertyBlock(properties); properties.SetColor("_BaseColor", color);
            confirmRenderer.SetPropertyBlock(properties);
        }
        private void RefreshStatusLight()
        {
            if (statusLight == null) return;
            if (statusProperties == null) statusProperties = new MaterialPropertyBlock();
            Color color = IsCompleted ? completedColor : pendingColor;
            statusLight.GetPropertyBlock(statusProperties);
            statusProperties.SetColor("_BaseColor", color);
            statusProperties.SetColor("_EmissionColor", Color.Lerp(color, Color.white, .14f) * emissionIntensity);
            statusLight.SetPropertyBlock(statusProperties);
            PuzzleStatusGlow.Apply(statusLight, color);
        }
        private void OnDisable()
        {
            // Settle an in-flight click without discarding the selected combination.
            for (int i = 0; i < 4; i++) if (turning[i])
            { turning[i] = false; angles[i] = -90f * puzzle.SymbolAt(i); if (rollers[i] != null) rollers[i].localRotation = Quaternion.Euler(angles[i], 0, 0); }
            pressRemaining = errorRemaining = 0;
            if (confirmVisual != null) confirmVisual.localPosition = confirmRest;
            RefreshButton();
        }
    }
}
