using GameJam2026.SymbolPassword;
using GameJam2026.Wires;
using UnityEngine;

// Physical red/green indicator for the boards that previously only had UI feedback.
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class PuzzleCompletionLight : MonoBehaviour
{
    [SerializeField] private WiresBoard wiresBoard;
    [SerializeField] private SymbolPasswordBoard passwordBoard;
    [SerializeField] private Color pendingColor = new Color(.65f, .16f, .12f);
    [SerializeField] private Color completedColor = new Color(.18f, .55f, .24f);
    [SerializeField, Min(0f)] private float emissionIntensity = .025f;
    private Renderer lightRenderer;
    private MaterialPropertyBlock properties;
    private bool? displayedCompletion;

    private void OnEnable()
    {
        lightRenderer = GetComponent<Renderer>();
        if (properties == null) properties = new MaterialPropertyBlock();
        displayedCompletion = null;
        Refresh();
    }

    // Reading the board state also handles restart and reactivation, without adding
    // completion listeners that could survive a disable/enable cycle.
    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        bool completed = wiresBoard != null ? wiresBoard.IsCompleted :
            passwordBoard != null && passwordBoard.IsCompleted;
        if (lightRenderer == null || properties == null || displayedCompletion == completed) return;
        displayedCompletion = completed;
        Color color = completed ? completedColor : pendingColor;
        lightRenderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_EmissionColor", Color.Lerp(color, Color.white, .14f) * emissionIntensity);
        lightRenderer.SetPropertyBlock(properties);
        PuzzleStatusGlow.Apply(lightRenderer, color);
    }
}
