using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GameJam2026.Hydraulics;
using GameJam2026.Levers;
using GameJam2026.LightSequence;
using GameJam2026.SymbolPassword;
using GameJam2026.Wires;
using GameJam2026.CelestialLock;

namespace GameJam2026
{
    [DisallowMultipleComponent, RequireComponent(typeof(CameraController))]
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField, Range(1, 5999)] private float initialSeconds = 600;
        [Header("Approaching planet")]
        [SerializeField] private Transform planet;
        [SerializeField, Min(1)] private float finalPlanetScale = 9f;
        [SerializeField] private Transform planetImpactPoint;
        [SerializeField, Min(0)] private float finalPlanetClearance = .05f;
        [Header("Time warnings (seconds remaining)")]
        [SerializeField, Min(0)] private float shakeStartsAt = 240f;
        [SerializeField, Min(0)] private float redWarningStartsAt = 60f;
        [SerializeField, Min(.1f)] private float warningPulseSeconds = 1.2f;
        [SerializeField] private UnityEvent onTimeExpired = new UnityEvent();
        [SerializeField] private UnityEvent onVictory = new UnityEvent();
        private CountdownClock clock;
        private CameraController controller;
        private SevenSegmentDisplay display;
        private RectTransform hud;
        private GameObject result;
        private Text resultText;
        private Text resultMessage;
        private Button replayButton;
        private Button menuButton;
        private static readonly Color SuccessBlue = new Color(.25f, .43f, 1f);
        private double lastTick;
        private Vector3 planetInitialScale;
        private Vector3 planetApproachDirection;
        private float planetInitialRadius;
        private float planetInitialClearance;
        private ScreenWarningOverlay warningOverlay;
        private HydraulicsBoard hydraulics;
        private CelestialLockBoard celestialLock;
        private LeversBoard levers;
        private LightSequenceBoard lights;
        private SymbolPasswordBoard password;
        private WiresBoard wires;
        public bool HasEnded { get; private set; }
        public bool HasWon { get; private set; }
        public bool IsReadyToFinish { get; private set; }
        public double RemainingSeconds => clock == null ? initialSeconds : clock.RemainingSeconds;

        private void Start()
        {
            controller = GetComponent<CameraController>();
            clock = new CountdownClock(initialSeconds);
            lastTick = Time.unscaledTimeAsDouble;
            hydraulics = FindFirstObjectByType<HydraulicsBoard>();
            celestialLock = FindFirstObjectByType<CelestialLockBoard>();
            levers = FindFirstObjectByType<LeversBoard>();
            lights = FindFirstObjectByType<LightSequenceBoard>();
            password = FindFirstObjectByType<SymbolPasswordBoard>();
            wires = FindFirstObjectByType<WiresBoard>();
            InitializePlanetApproach();
            BuildHud();
            Listen(CheckCompletion, true);
        }

        private void Update()
        {
            // Also catches completion already present when this component started.
            CheckCompletion();
        }

        private void Tick()
        {
            if (HasEnded || clock == null) return;
            double now = Time.unscaledTimeAsDouble;
            clock.Advance(now - lastTick);
            lastTick = now;
            display.SetSeconds(clock.DisplaySeconds);
            UpdateTimeEffects();
            if (clock.IsExpired) End(false);
        }

        private void UpdateTimeEffects()
        {
            float remaining = (float)clock.RemainingSeconds;
            float progress = Mathf.Clamp01(1f - remaining / initialSeconds);
            if (planet != null)
            {
                float scale = Mathf.Lerp(1f, finalPlanetScale, progress);
                planet.localScale = planetInitialScale * scale;
                if (planetImpactPoint != null && planetInitialRadius > 0f)
                {
                    float clearance = Mathf.Lerp(planetInitialClearance, finalPlanetClearance, progress);
                    planet.position = planetImpactPoint.position + planetApproachDirection *
                        (planetInitialRadius * scale + clearance);
                }
            }

            float shake = remaining <= shakeStartsAt && shakeStartsAt > 0f
                ? Mathf.SmoothStep(0f, 1f, (shakeStartsAt - remaining) / 5f) : 0f;
            controller.SetTimeShake(shake);
            float alpha = 0f;
            if (remaining <= redWarningStartsAt && redWarningStartsAt > 0f)
            {
                // The warning starts visibly red and fades in/out at a steady siren rhythm.
                double elapsed = redWarningStartsAt - remaining;
                float pulse = .5f + .5f * Mathf.Cos((float)(elapsed % warningPulseSeconds) *
                    Mathf.PI * 2f / warningPulseSeconds);
                alpha = Mathf.Lerp(.06f, .48f, pulse);
            }
            warningOverlay.color = new Color(1f, .015f, 0f, alpha);
        }

        private void InitializePlanetApproach()
        {
            if (planet == null) return;
            planetInitialScale = planet.localScale;
            if (planetImpactPoint == null) return;
            var sphere = planet.GetComponent<SphereCollider>();
            if (sphere == null || sphere.center.sqrMagnitude > .000001f)
            {
                Debug.LogWarning("Planet approach requires a centered SphereCollider to measure surface clearance.", this);
                return;
            }
            Vector3 worldScale = planet.lossyScale;
            planetInitialRadius = sphere.radius * Mathf.Max(Mathf.Abs(worldScale.x),
                Mathf.Abs(worldScale.y), Mathf.Abs(worldScale.z));
            Vector3 offset = planet.position - planetImpactPoint.position;
            planetApproachDirection = offset.sqrMagnitude > .000001f ? offset.normalized : Vector3.forward;
            planetInitialClearance = Mathf.Max(0, offset.magnitude - planetInitialRadius);
        }

        private void CheckCompletion()
        {
            Tick(); // The deadline takes precedence over an input received after it.
            bool replacementCompleted = celestialLock != null ? celestialLock.IsCompleted : hydraulics != null && hydraulics.IsCompleted;
            if (!HasEnded && replacementCompleted && levers != null && lights != null &&
                password != null && wires != null && levers.IsCompleted &&
                lights.IsCompleted && password.IsCompleted && wires.IsCompleted)
                IsReadyToFinish = true;
            else IsReadyToFinish = false;
        }

        public bool TryFinishGame()
        {
            CheckCompletion();
            if (!IsReadyToFinish) return false;
            End(true);
            return true;
        }

        private void End(bool won)
        {
            if (HasEnded) return;
            HasEnded = true;
            HasWon = won;
            IsReadyToFinish = false;
            warningOverlay.color = Color.clear;
            controller.EndGame();
            resultText.text = won ? "Congratulations!" : "Time's Up";
            resultMessage.text = won ? "You won the game!" : "The ship has crashed";
            Color resultColor = won ? SuccessBlue : new Color(1, .33f, .23f);
            resultText.color = resultMessage.color = resultColor;
            replayButton.GetComponentInChildren<Text>().text = won ? "Play Again" : "Try Again";
            foreach (var button in new[] { replayButton, menuButton })
            {
                button.GetComponentInChildren<Text>().color = resultColor;
                button.GetComponent<Image>().color = won
                    ? new Color(.055f, .10f, .29f) : new Color(.19f, .075f, .06f);
            }
            result.SetActive(true);
            hud.SetAsLastSibling();
            if (won)
            {
                SoundManager.Play(GameSound.Victory);
                onVictory.Invoke();
            }
            else onTimeExpired.Invoke();
        }

        private void Listen(UnityAction callback, bool subscribe)
        {
            if (hydraulics != null) Bind(hydraulics.OnCompleted, callback, subscribe);
            if (celestialLock != null) Bind(celestialLock.OnCompleted, callback, subscribe);
            if (levers != null) Bind(levers.OnCompleted, callback, subscribe);
            if (lights != null) Bind(lights.OnCompleted, callback, subscribe);
            if (password != null) Bind(password.OnCompleted, callback, subscribe);
            if (wires != null) Bind(wires.OnCompleted, callback, subscribe);
        }

        private static void Bind(UnityEvent signal, UnityAction callback, bool subscribe)
        {
            if (subscribe) signal.AddListener(callback); else signal.RemoveListener(callback);
        }

        private void OnDestroy() => Listen(CheckCompletion, false);

        private void BuildHud()
        {
            var canvasObject = new GameObject("GameSessionHUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            RectTransform warning = Box("Siren borders", canvasObject.transform, Vector2.zero, Vector2.zero);
            warning.anchorMin = Vector2.zero;
            warning.anchorMax = Vector2.one;
            warning.offsetMin = warning.offsetMax = Vector2.zero;
            warningOverlay = warning.gameObject.AddComponent<ScreenWarningOverlay>();
            warningOverlay.color = Color.clear;
            warningOverlay.raycastTarget = false;
            hud = Box("Countdown", canvasObject.transform, new Vector2(300, 104), new Vector2(0, -24));
            hud.anchorMin = hud.anchorMax = new Vector2(.5f, 1);
            hud.pivot = new Vector2(.5f, 1);
            Image background = hud.gameObject.AddComponent<Image>();
            background.color = new Color(.018f, .023f, .027f, .97f);
            background.raycastTarget = false;
            RectTransform digits = Box("Red LED digits", hud, new Vector2(264, 80), Vector2.zero);
            display = digits.gameObject.AddComponent<SevenSegmentDisplay>();
            display.color = new Color(1, .055f, .025f);
            display.raycastTarget = false;
            display.SetSeconds(clock.DisplaySeconds);

            RectTransform overlay = Box("Result", canvasObject.transform, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .86f);
            resultText = Label("", overlay, new Vector2(900, 90), new Vector2(0, 78), 52);
            resultMessage = Label("", overlay, new Vector2(900, 44), new Vector2(0, 12), 26);
            replayButton = MakeButton("Try Again", overlay, new Vector2(0, -60),
                () => SceneManager.LoadScene(gameObject.scene.name));
            menuButton = MakeButton("Back to Menu", overlay, new Vector2(0, -135),
                () => SceneManager.LoadScene("MainMenu"));
            result = overlay.gameObject;
            result.SetActive(false);
        }

        private static RectTransform Box(string title, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Text Label(string text, Transform parent, Vector2 size, Vector2 position, int fontSize)
        {
            Text label = Box(text, parent, size, position).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1, .33f, .23f);
            label.raycastTarget = false;
            return label;
        }

        private static Button MakeButton(string text, Transform parent, Vector2 position, UnityAction action)
        {
            RectTransform rect = Box(text, parent, new Vector2(370, 58), position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.19f, .075f, .06f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Label(text, rect, rect.sizeDelta, Vector2.zero, 23);
            return button;
        }
    }
}
