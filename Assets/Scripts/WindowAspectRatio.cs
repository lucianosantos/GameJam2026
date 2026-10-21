using UnityEngine;

namespace GameJam2026
{
    // Keep the desktop window at the same aspect as the authored cameras and UI.
    // Wait for resizing to settle instead of fighting the operating system while dragging.
    public sealed class WindowAspectRatio : MonoBehaviour
    {
        private Vector2Int observed;
        private Vector2Int settled = new Vector2Int(1280, 720);
        private double lastResize;
        private bool pending = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Application.isEditor || (Application.platform != RuntimePlatform.WindowsPlayer &&
                Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.LinuxPlayer)) return;
            if (FindFirstObjectByType<WindowAspectRatio>() != null) return;
            var owner = new GameObject("Window aspect ratio");
            DontDestroyOnLoad(owner);
            owner.AddComponent<WindowAspectRatio>();
        }

        private void Awake()
        {
            observed = new Vector2Int(Screen.width, Screen.height);
            lastResize = Time.unscaledTimeAsDouble;
        }

        private void Update()
        {
            var current = new Vector2Int(Screen.width, Screen.height);
            if (Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                observed = current;
                pending = true;
                lastResize = Time.unscaledTimeAsDouble;
                return;
            }
            if (current.x <= 0 || current.y <= 0) return;
            if (current != observed)
            {
                observed = current;
                lastResize = Time.unscaledTimeAsDouble;
                pending = true;
            }
            if (!pending || Time.unscaledTimeAsDouble - lastResize < .2d) return;
            pending = false;
            // Permit a single pixel of rounding by the window manager.
            if (Mathf.Abs(current.x * 9f - current.y * 16f) <= 16f)
            {
                settled = current;
                return;
            }

            var area = Screen.mainWindowDisplayInfo.workArea;
            // Leave room for the native window borders and title bar.
            var maximum = new Vector2Int(Mathf.Max(16, area.width - 32), Mathf.Max(9, area.height - 64));
            var target = ConstrainSize(current, settled, maximum);
            settled = target;
            Screen.SetResolution(target.x, target.y, FullScreenMode.Windowed);
        }

        public static Vector2Int ConstrainSize(Vector2Int requested, Vector2Int previous, Vector2Int maximum)
        {
            float widthChange = Mathf.Abs(requested.x - previous.x) / (float)Mathf.Max(1, previous.x);
            float heightChange = Mathf.Abs(requested.y - previous.y) / (float)Mathf.Max(1, previous.y);
            float units = widthChange >= heightChange ? requested.x / 16f : requested.y / 9f;
            int maxUnits = Mathf.Max(1, Mathf.Min(maximum.x / 16, maximum.y / 9));
            int count = Mathf.Clamp(Mathf.RoundToInt(units), 1, maxUnits);
            return new Vector2Int(count * 16, count * 9);
        }
    }
}
