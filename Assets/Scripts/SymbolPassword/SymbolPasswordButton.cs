using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GameJam2026.SymbolPassword
{
    public enum PasswordKeyAction { Digit, Clear, Submit }

    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class SymbolPasswordButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Renderer keyRenderer;
        [SerializeField] private Transform[] legends;
        [SerializeField] private PasswordKeyAction action;
        [SerializeField, Range(0, 9)] private int digit;
        [SerializeField] private Vector3 pressedOffset = new Vector3(0, 0, .008f);
        [SerializeField, Min(.001f)] private float pressDuration = .06f;
        [SerializeField, Min(.001f)] private float releaseDuration = .10f;
        [SerializeField, Min(0)] private float minimumPressTime = .08f;
        [SerializeField] private UnityEvent onClick = new UnityEvent();
        private SymbolPasswordBoard board;
        private BoxCollider hitArea;
        private Vector3 restPosition;
        private Vector3[] legendRestPositions;
        private MaterialPropertyBlock properties;
        private bool held, animating;
        private int pointerId;
        private float pressTime, amount;

        public PasswordKeyAction Action => action;
        public int Digit => digit;
        public UnityEvent OnClick => onClick;
        public bool CanInteract => isActiveAndEnabled && board != null && board.CanInteract;

        private void Awake()
        {
            hitArea = GetComponent<BoxCollider>();
            restPosition = visual.localPosition;
            legendRestPositions = new Vector3[legends == null ? 0 : legends.Length];
            for (int i = 0; i < legendRestPositions.Length; i++)
                if (legends[i] != null) legendRestPositions[i] = legends[i].localPosition;
            properties = new MaterialPropertyBlock();
        }

        private void OnEnable() { board = GetComponentInParent<SymbolPasswordBoard>(); }
        private void OnTransformParentChanged() { board = GetComponentInParent<SymbolPasswordBoard>(); }

        private void Update()
        {
            hitArea.enabled = CanInteract;
            // Let the final/Ok click finish its travel even when it blocks further input.
            if (!CanInteract) held = false;
            bool down = held || (animating && Time.unscaledTime - pressTime < minimumPressTime);
            amount = Mathf.MoveTowards(amount, down ? 1f : 0f,
                Time.unscaledDeltaTime / (down ? pressDuration : releaseDuration));
            ApplyOffset(pressedOffset * (amount * amount * (3f - 2f * amount)));
            if (!down && amount == 0) animating = false;
        }

        public void SetError(bool active, Color color)
        {
            if (keyRenderer == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            keyRenderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", active ? color : keyRenderer.sharedMaterial.GetColor("_BaseColor"));
            properties.SetColor("_EmissionColor", active ? color * 1.5f : Color.black);
            keyRenderer.SetPropertyBlock(properties);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!CanInteract || held || e.button != PointerEventData.InputButton.Left) return;
            pointerId = e.pointerId; held = animating = true; pressTime = Time.unscaledTime;
            SoundManager.Play(GameSound.PasswordKey);
        }
        public void OnPointerUp(PointerEventData e)
        { if (e.button == PointerEventData.InputButton.Left && e.pointerId == pointerId) held = false; }
        public void OnPointerExit(PointerEventData e) { if (e.pointerId == pointerId) held = false; }
        public void OnPointerClick(PointerEventData e)
        {
            if (!CanInteract || e.button != PointerEventData.InputButton.Left) return;
            board.PressKey(this);
            onClick.Invoke();
        }
        private void ApplyOffset(Vector3 offset)
        {
            if (visual != null) visual.localPosition = restPosition + offset;
            if (legendRestPositions != null)
                for (int i = 0; i < legendRestPositions.Length; i++)
                    if (legends[i] != null) legends[i].localPosition = legendRestPositions[i] + offset;
        }
        private void OnDisable()
        {
            if (hitArea != null) hitArea.enabled = false;
            held = animating = false; amount = 0;
            ApplyOffset(Vector3.zero);
        }
    }
}
