using UnityEngine;
using UnityEngine.UI;

namespace GameJam2026.Hydraulics
{
    [RequireComponent(typeof(Button))]
    public sealed class HydraulicsTileView : MonoBehaviour
    {
        [SerializeField] private Image pipeImage;
        [SerializeField] private Image backgroundImage;
        private Button button;
        private HydraulicsBoard board;
        private int index;

        public bool IsConfigured => pipeImage != null && backgroundImage != null &&
                                    pipeImage != backgroundImage;

        public void Initialize(HydraulicsBoard owner, int tileIndex)
        {
            board = owner;
            index = tileIndex;
            button = GetComponent<Button>();
            button.onClick.AddListener(Click);
            // The root Button receives the click, including on a transparent pipe pixel.
            pipeImage.raycastTarget = false;
            backgroundImage.raycastTarget = true;
        }

        public void Render(Sprite sprite, int rotation, bool locked, Color background, Rect normalizedRect)
        {
            pipeImage.sprite = sprite;
            pipeImage.preserveAspect = false;
            RectTransform pipeRect = pipeImage.rectTransform;
            pipeRect.anchorMin = normalizedRect.min;
            pipeRect.anchorMax = normalizedRect.max;
            // Rotate the offset image around the cell center, rather than its own center.
            pipeRect.pivot = new Vector2(
                (0.5f - normalizedRect.x) / normalizedRect.width,
                (0.5f - normalizedRect.y) / normalizedRect.height);
            pipeRect.sizeDelta = Vector2.zero;
            pipeRect.anchoredPosition = Vector2.zero;
            pipeRect.localRotation = Quaternion.Euler(0, 0, -90 * rotation);
            backgroundImage.color = background;
            button.interactable = !locked;
        }

        private void Click() => board.RotateTile(index);
    }
}
