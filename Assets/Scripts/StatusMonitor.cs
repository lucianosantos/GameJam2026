using GameJam2026.CelestialLock;
using GameJam2026.Levers;
using GameJam2026.LightSequence;
using GameJam2026.SymbolPassword;
using GameJam2026.Wires;
using UnityEngine;
using UnityEngine.UI;

namespace GameJam2026
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class StatusMonitor : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private Font font;
        [SerializeField] private Color nameColor = new Color(.2f, .2f, .2f);
        [SerializeField] private Color failColor = new Color(.75f, .045f, .035f);
        [SerializeField] private Color okColor = new Color(.035f, .42f, .12f);
        [SerializeField] private Camera displayCamera;
        [Header("Rows: Zodiac, Wires, Password, Levers, Lock")]
        [SerializeField] private Text[] names = new Text[5];
        [SerializeField] private Text[] statuses = new Text[5];
        [Header("Minigames")]
        [SerializeField] private LightSequenceBoard zodiac;
        [SerializeField] private WiresBoard wires;
        [SerializeField] private SymbolPasswordBoard password;
        [SerializeField] private LeversBoard levers;
        [SerializeField] private CelestialLockBoard celestialLock;

        private void OnEnable() => Refresh();
        private void LateUpdate()
        {
            Refresh();
            // RenderTextures lose their contents on import/reload. Keep the edit-mode preview alive.
            if (!Application.isPlaying && displayCamera != null)
            {
                Canvas.ForceUpdateCanvases();
                displayCamera.Render();
            }
        }

        // Reading the boards also handles puzzle restarts, which have no completion event.
        public void Refresh()
        {
            ApplyAppearance();
            SetStatus(0, zodiac != null && zodiac.IsCompleted);
            SetStatus(1, wires != null && wires.IsCompleted);
            SetStatus(2, password != null && password.IsCompleted);
            SetStatus(3, levers != null && levers.IsCompleted);
            SetStatus(4, celestialLock != null && celestialLock.IsCompleted);
        }

        private void OnValidate() => ApplyAppearance();

        private void ApplyAppearance()
        {
            foreach (var label in names)
            {
                if (label == null) continue;
                if (font != null) label.font = font;
                label.color = nameColor;
            }
            foreach (var label in statuses)
            {
                if (label == null) continue;
                if (font != null) label.font = font;
                label.color = label.text == "OK" ? okColor : failColor;
            }
        }

        private void SetStatus(int index, bool completed)
        {
            if (index >= statuses.Length || statuses[index] == null) return;
            var label = statuses[index];
            label.text = completed ? "OK" : "FAIL";
            label.color = completed ? okColor : failColor;
        }
    }
}
