using UnityEngine;
using UnityEngine.EventSystems;

namespace GameJam2026.Levers
{
    [DisallowMultipleComponent]
    public sealed class LeverHandle : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Transform movingPivot;
        [SerializeField] private float bottomAngle = -40f;
        [SerializeField] private float topAngle = -7f;
        [SerializeField, Min(.001f)] private float dragTravel = .28f;
        private LeversBoard board;
        private bool dragging;
        private float dragStart;
        private int startPosition;
        public int Position { get; private set; } = 1;
        public bool HasPivot => movingPivot != null;
        public void Bind(LeversBoard owner) { board = owner; dragging = false; }
        public void ShowPosition(int position)
        {
            Position = position;
            if (movingPivot != null)
            {
                movingPivot.localPosition = Vector3.zero;
                movingPivot.localRotation = Quaternion.Euler(Mathf.Lerp(bottomAngle, topAngle, (position - 1) / 3f), 0, 0);
            }
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (data.dragging || board == null || !board.CanInteract) return;
            if (data.button == PointerEventData.InputButton.Left) board.SetPosition(this, Position % 4 + 1);
            else if (data.button == PointerEventData.InputButton.Right) board.SetPosition(this, (Position + 2) % 4 + 1);
        }

        private bool PointerPosition(PointerEventData data, out float distance)
        {
            distance = 0;
            var camera = data.pressEventCamera;
            if (camera == null) return false;
            var plane = new Plane(camera.transform.forward, transform.position);
            Ray ray = camera.ScreenPointToRay(data.position);
            if (!plane.Raycast(ray, out float enter)) return false;
            Vector3 axis = Vector3.ProjectOnPlane(transform.forward, plane.normal).normalized;
            if (axis.sqrMagnitude < .001f) return false;
            distance = Vector3.Dot(ray.GetPoint(enter) - transform.position, axis);
            return true;
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || board == null || !board.CanInteract) return;
            dragging = PointerPosition(data, out dragStart);
            startPosition = Position;
            if (dragging) data.eligibleForClick = false;
        }
        public void OnDrag(PointerEventData data)
        {
            if (!dragging || board == null || !board.CanInteract) { dragging = false; return; }
            if (PointerPosition(data, out float distance))
                board.SetPosition(this, Mathf.Clamp(startPosition + Mathf.RoundToInt((distance - dragStart) / dragTravel * 3f), 1, 4));
        }
        public void OnEndDrag(PointerEventData data) { dragging = false; }
        private void OnDisable() { dragging = false; }
    }
}
