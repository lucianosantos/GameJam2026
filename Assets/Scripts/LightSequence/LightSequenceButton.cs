using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GameJam2026.LightSequence
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LightSequenceButton : MonoBehaviour, IPointerDownHandler,
        IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Transform visual;
        [Tooltip("Movement in the prefab root's local space, into the panel.")]
        [SerializeField] private Vector3 pressedOffset = new Vector3(0, 0, 0.008f);
        [SerializeField, Min(0.001f)] private float pressDuration = 0.06f;
        [SerializeField, Min(0.001f)] private float releaseDuration = 0.10f;
        [SerializeField, Min(0f)] private float minimumPressTime = 0.08f;
        [SerializeField] private bool interactable = true;
        [Header("Sequence and light (configured in the prefab)")]
        [SerializeField, Min(1)] private int buttonId = 1;
        [SerializeField] private Renderer lightRenderer;
        [SerializeField] private Color correctColor = new Color(0.05f, 1f, 0.1f);
        [SerializeField] private Color errorColor = new Color(1f, 0.02f, 0.02f);
        [SerializeField, Min(0f)] private float emissionIntensity = 1.5f;
        [SerializeField, Min(1)] private int errorFlashCount = 3;
        [SerializeField, Min(0.01f)] private float errorFlashDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float errorFlashInterval = 0.10f;
        [SerializeField] private UnityEvent onClick = new UnityEvent();

        private BoxCollider hitArea;
        private LightSequenceInput input;
        private Vector3 restPosition;
        private float amount;
        private float pressTime;
        private bool held;
        private bool animatingPress;
        private int pointerId;
        private LightSequenceBoard board;
        private MaterialPropertyBlock lightProperties;
        private Color offColor;
        private Color offEmission;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public int ButtonId => buttonId;
        public bool HasLight => lightRenderer != null;
        public bool IsLitCorrect { get; private set; }
        public UnityEvent OnClick => onClick;
        public bool Interactable { get => interactable; set => interactable = value; }
        public bool CanInteract => interactable && isActiveAndEnabled &&
                                   (input == null || input.CanInteract) &&
                                   (board == null || board.CanAcceptInput);

        private void Awake()
        {
            hitArea = GetComponent<BoxCollider>();
            if (visual != null) restPosition = visual.localPosition;
            lightProperties = new MaterialPropertyBlock();
            if (lightRenderer != null && lightRenderer.sharedMaterial != null)
            {
                offColor = lightRenderer.sharedMaterial.GetColor(BaseColor);
                // The shared material keeps emission enabled; each instance starts unlit.
                offEmission = Color.black;
            }
            SetOff();
        }

        private void OnEnable()
        {
            input = GetComponentInParent<LightSequenceInput>();
            board = GetComponentInParent<LightSequenceBoard>();
            if (hitArea != null) hitArea.enabled = CanInteract;
        }

        private void OnTransformParentChanged()
        {
            input = GetComponentInParent<LightSequenceInput>();
            board = GetComponentInParent<LightSequenceBoard>();
        }

        public void SetCorrect()
        {
            IsLitCorrect = true;
            ApplyLight(correctColor, correctColor * emissionIntensity);
        }

        public void SetOff()
        {
            IsLitCorrect = false;
            ApplyLight(offColor, offEmission);
        }

        // The board owns the coroutine; all visual choices remain on this prefab.
        public IEnumerator FlashError()
        {
            IsLitCorrect = false;
            for (int i = 0; i < errorFlashCount; i++)
            {
                ApplyLight(errorColor, errorColor * emissionIntensity);
                yield return new WaitForSecondsRealtime(errorFlashDuration);
                SetOff();
                if (i + 1 < errorFlashCount) yield return new WaitForSecondsRealtime(errorFlashInterval);
            }
        }

        private void ApplyLight(Color color, Color emission)
        {
            if (lightRenderer == null || lightProperties == null) return;
            lightRenderer.GetPropertyBlock(lightProperties);
            lightProperties.SetColor(BaseColor, color);
            lightProperties.SetColor(EmissionColor, emission);
            lightRenderer.SetPropertyBlock(lightProperties);
        }

        private void Update()
        {
            hitArea.enabled = CanInteract;
            if (!CanInteract)
            {
                ResetVisual();
                return;
            }

            bool down = held || (animatingPress && Time.unscaledTime - pressTime < minimumPressTime);
            float duration = down ? pressDuration : releaseDuration;
            amount = Mathf.MoveTowards(amount, down ? 1f : 0f,
                Time.unscaledDeltaTime / Mathf.Max(0.001f, duration));
            float eased = amount * amount * (3f - 2f * amount);
            if (visual != null) visual.localPosition = restPosition + pressedOffset * eased;
            if (!down && amount == 0f) animatingPress = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanInteract || held || eventData.button != PointerEventData.InputButton.Left) return;
            pointerId = eventData.pointerId;
            held = true;
            animatingPress = true;
            pressTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && eventData.pointerId == pointerId)
                held = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId) held = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (CanInteract && eventData.button == PointerEventData.InputButton.Left) onClick.Invoke();
        }

        private void OnDisable()
        {
            if (hitArea != null) hitArea.enabled = false;
            ResetVisual();
        }

        private void ResetVisual()
        {
            held = false;
            animatingPress = false;
            amount = 0;
            if (visual != null) visual.localPosition = restPosition;
        }
    }
}
