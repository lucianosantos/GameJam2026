using UnityEngine;
using UnityEngine.EventSystems;

namespace GameJam2026.Levers
{
    [DisallowMultipleComponent]
    public sealed class LeverHandle : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Transform movingPivot;
        [SerializeField] private float bottomAngle = -40f;
        [SerializeField] private float topAngle = -7f;
        private LeversBoard board;
        public int Position { get; private set; } = 1;
        public bool HasPivot => movingPivot != null;
        public void Bind(LeversBoard owner) { board = owner; }
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

    }
}
