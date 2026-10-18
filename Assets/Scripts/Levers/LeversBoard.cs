using UnityEngine;
using UnityEngine.Events;

namespace GameJam2026.Levers
{
    [DisallowMultipleComponent]
    public sealed class LeversBoard : MonoBehaviour
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private LeverHandle[] levers;
        [Tooltip("Positions 1 (bottom) to 4 (top), from left to right. Prototype solution; match it to the manual.")]
        [SerializeField] private int[] correctPositions = { 1, 2, 3 };
        [SerializeField] private Renderer statusLight;
        [SerializeField] private Color pendingColor = new Color(1f, .055f, .015f);
        [SerializeField] private Color completedColor = new Color(.035f, 1f, .19f);
        [SerializeField, Min(0)] private float emissionIntensity = 2.4f;
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();
        private LeversPuzzle puzzle;
        private MaterialPropertyBlock properties;
        public bool IsCompleted => puzzle != null && puzzle.IsCompleted;
        public bool CanInteract => isActiveAndEnabled && puzzle != null && !IsCompleted &&
            cameraController != null && cameraController.IsFocusedOn(cameraTarget);
        public UnityEvent OnCompleted => onCompleted;

        private void Awake() { RestartPuzzle(); }

        public void RestartPuzzle()
        {
            puzzle = null;
            RefreshLight();
            if (levers == null || levers.Length != 3 || statusLight == null ||
                cameraController == null || cameraTarget == null)
            { Debug.LogError("Levers: configure three handles, camera/target and status light Renderer.", this); return; }
            for (int i = 0; i < levers.Length; i++)
            {
                if (levers[i] == null || !levers[i].HasPivot || System.Array.IndexOf(levers, levers[i]) != i)
                { Debug.LogError("Levers: assign three unique handles with moving pivots.", this); return; }
            }
            try { puzzle = new LeversPuzzle(correctPositions); }
            catch (System.ArgumentException error) { Debug.LogError("Levers: " + error.Message, this); return; }
            for (int i = 0; i < levers.Length; i++)
            { levers[i].Bind(this); levers[i].ShowPosition(puzzle.GetPosition(i)); }
        }

        public void SetPosition(LeverHandle handle, int position)
        {
            if (!CanInteract || handle == null) return;
            int index = System.Array.IndexOf(levers, handle);
            if (index < 0 || !puzzle.SetPosition(index, position)) return;
            handle.ShowPosition(puzzle.GetPosition(index));
            if (!IsCompleted) return;
            RefreshLight();
            SoundManager.Play(GameSound.PuzzleCompleted);
            onCompleted.Invoke();
        }

        private void RefreshLight()
        {
            if (statusLight == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            Color color = IsCompleted ? completedColor : pendingColor;
            statusLight.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_EmissionColor", Color.Lerp(color, Color.white, .14f) * emissionIntensity);
            statusLight.SetPropertyBlock(properties);
            PuzzleStatusGlow.Apply(statusLight, color);
        }
    }
}
