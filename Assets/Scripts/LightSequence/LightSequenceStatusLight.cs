using UnityEngine;

namespace GameJam2026.LightSequence
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class LightSequenceStatusLight : MonoBehaviour
    {
        [SerializeField] private LightSequenceBoard board;
        [SerializeField] private Renderer lightRenderer;
        [SerializeField] private Color pendingColor = new Color(1f, .055f, .015f);
        [SerializeField] private Color completedColor = new Color(.035f, 1f, .19f);
        [SerializeField, Min(0f)] private float emissionIntensity = 2.4f;
        private MaterialPropertyBlock properties;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void OnEnable()
        {
            if (lightRenderer == null) lightRenderer = GetComponent<Renderer>();
            properties = new MaterialPropertyBlock();
            if (board != null) board.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (board != null) board.StateChanged -= Refresh;
        }

        public void Refresh()
        {
            if (lightRenderer == null || properties == null) return;
            Color color = board != null && board.IsCompleted ? completedColor : pendingColor;
            lightRenderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColor, color);
            properties.SetColor(EmissionColor, Color.Lerp(color, Color.white, .14f) * emissionIntensity);
            lightRenderer.SetPropertyBlock(properties);
            PuzzleStatusGlow.Apply(lightRenderer, color);
        }
    }
}
