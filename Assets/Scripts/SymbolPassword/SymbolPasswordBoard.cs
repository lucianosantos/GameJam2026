using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameJam2026.SymbolPassword
{
    [DisallowMultipleComponent]
    public sealed class SymbolPasswordBoard : MonoBehaviour
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private SymbolPasswordButton[] buttons;
        [SerializeField] private Sprite[] digitSymbols;
        [SerializeField] private Image[] displaySlots;
        [SerializeField] private Image monitor;
        [SerializeField] private int[] correctOrder = { 4, 1, 0, 3, 6 };
        [SerializeField] private Color monitorColor = new Color(.72f, .84f, .70f);
        [SerializeField] private Color errorColor = new Color(1f, .03f, .03f);
        [SerializeField, Min(1)] private int errorFlashCount = 3;
        [SerializeField, Min(.01f)] private float errorFlashDuration = .12f;
        [SerializeField, Min(.01f)] private float errorFlashInterval = .1f;
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();
        private SymbolPasswordPuzzle puzzle;
        private Coroutine errorRoutine;
        private bool showingError;

        public UnityEvent OnCompleted => onCompleted;
        public bool IsCompleted => puzzle != null && puzzle.IsCompleted;
        public int Progress => puzzle == null ? 0 : puzzle.Entered.Count;
        public bool CanInteract => isActiveAndEnabled && puzzle != null && !IsCompleted && !showingError &&
            cameraController != null && cameraController.IsFocusedOn(cameraTarget);

        private void Awake() { RestartPuzzle(); }

        public void PressKey(SymbolPasswordButton button)
        {
            if (!CanInteract || button == null || System.Array.IndexOf(buttons, button) < 0) return;
            switch (button.Action)
            {
                case PasswordKeyAction.Digit:
                    puzzle.Enter(button.Digit);
                    RefreshDisplay();
                    break;
                case PasswordKeyAction.Clear:
                    puzzle.Clear();
                    RefreshDisplay();
                    break;
                case PasswordKeyAction.Submit:
                    if (puzzle.Submit())
                    {
                        RefreshMonitor(false);
                        SoundManager.Play(GameSound.PuzzleCompleted);
                        onCompleted.Invoke();
                    }
                    else
                    {
                        showingError = true;
                        errorRoutine = StartCoroutine(FlashError());
                    }
                    break;
            }
        }

        private IEnumerator FlashError()
        {
            for (int i = 0; i < errorFlashCount; i++)
            {
                SetError(true);
                yield return new WaitForSecondsRealtime(errorFlashDuration);
                SetError(false);
                if (i + 1 < errorFlashCount) yield return new WaitForSecondsRealtime(errorFlashInterval);
            }
            showingError = false;
            errorRoutine = null;
        }

        private void SetError(bool active)
        {
            RefreshMonitor(active);
            if (buttons != null) foreach (var button in buttons) if (button != null) button.SetError(active, errorColor);
        }

        private void RefreshMonitor(bool error)
        {
            if (monitor == null) return;
            monitor.color = error ? errorColor : IsCompleted ? new Color(.52f, 1f, .65f) : monitorColor;
            PuzzleStatusGlow.Apply(monitor, IsCompleted && !error);
        }

        private void RefreshDisplay()
        {
            for (int i = 0; i < displaySlots.Length; i++)
            {
                bool occupied = i < puzzle.Entered.Count;
                displaySlots[i].sprite = occupied ? digitSymbols[puzzle.Entered[i]] : null;
                displaySlots[i].enabled = occupied;
            }
        }

        public void RestartPuzzle()
        {
            puzzle = null;
            CancelError();
            if (correctOrder == null || correctOrder.Length == 0 || displaySlots == null ||
                displaySlots.Length != correctOrder.Length || digitSymbols == null || digitSymbols.Length != 10 ||
                monitor == null || buttons == null || buttons.Length != 12)
            { Debug.LogError("Symbol Password: configure twelve keys, ten symbols, a monitor and one display slot per password digit.", this); return; }
            var digits = new bool[10];
            int clearKeys = 0, submitKeys = 0;
            foreach (var button in buttons)
            {
                if (button == null) { ConfigurationError(); return; }
                if (button.Action == PasswordKeyAction.Clear) clearKeys++;
                else if (button.Action == PasswordKeyAction.Submit) submitKeys++;
                else
                {
                    if (button.Digit < 0 || button.Digit > 9 || digits[button.Digit]) { ConfigurationError(); return; }
                    digits[button.Digit] = true;
                }
            }
            if (clearKeys != 1 || submitKeys != 1) { ConfigurationError(); return; }
            foreach (var sprite in digitSymbols) if (sprite == null) { ConfigurationError(); return; }
            foreach (var slot in displaySlots) if (slot == null) { ConfigurationError(); return; }
            foreach (var digit in correctOrder) if (digit < 0 || digit > 9) { ConfigurationError(); return; }
            puzzle = new SymbolPasswordPuzzle(correctOrder);
            RefreshDisplay();
        }

        private void ConfigurationError()
        { Debug.LogError("Symbol Password: assign each digit 0-9 once, one Clear key, one Submit key, all symbol sprites/display slots and a valid solution.", this); }

        private void CancelError()
        {
            if (errorRoutine != null) StopCoroutine(errorRoutine);
            errorRoutine = null;
            showingError = false;
            SetError(false);
        }

        private void OnDisable() { CancelError(); }
    }
}
