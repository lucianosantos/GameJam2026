using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GameJam2026.LightSequence
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LightSequenceInput))]
    public sealed class LightSequenceBoard : MonoBehaviour
    {
        [SerializeField] private LightSequenceButton[] buttons;
        [Tooltip("Button IDs in the required order. Edit this array to change the solution.")]
        // Manual months 12,3,2,6,8,11,5,7 mapped to the eight zodiac keys.
        [SerializeField] private int[] correctOrder = { 8, 2, 1, 4, 6, 7, 3, 5 };
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();

        private LightSequenceInput input;
        private LightSequencePuzzle puzzle;
        private UnityAction[] clickHandlers;
        private Coroutine errorRoutine;
        private bool showingError;

        public UnityEvent OnCompleted => onCompleted;
        public event System.Action StateChanged;
        public bool IsCompleted => puzzle != null && puzzle.IsCompleted;
        public int Progress => puzzle != null ? puzzle.Progress : 0;
        public bool CanAcceptInput => isActiveAndEnabled && puzzle != null &&
                                      !puzzle.IsCompleted && !showingError;

        private void Awake()
        {
            input = GetComponent<LightSequenceInput>();
            RestartPuzzle();
        }

        private void OnEnable()
        {
            if (buttons == null) return;
            clickHandlers = new UnityAction[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null) continue;
                clickHandlers[i] = () => PressButton(button);
                button.OnClick.AddListener(clickHandlers[i]);
            }
        }

        private void OnDisable()
        {
            if (clickHandlers != null)
                for (int i = 0; i < clickHandlers.Length; i++)
                    if (buttons[i] != null && clickHandlers[i] != null)
                        buttons[i].OnClick.RemoveListener(clickHandlers[i]);
            CancelError();
        }

        public void RestartPuzzle()
        {
            CancelError();
            puzzle = null;
            TurnOffButtons();
            if (!ValidateConfiguration())
            {
                Debug.LogError("Light Sequence: assign unique positive Button IDs, a light Renderer on each prefab, and a nonempty Correct Order containing only assigned IDs.", this);
                StateChanged?.Invoke();
                return;
            }
            puzzle = new LightSequencePuzzle(correctOrder);
            StateChanged?.Invoke();
        }

        private bool ValidateConfiguration()
        {
            if (buttons == null || buttons.Length == 0 || correctOrder == null || correctOrder.Length == 0)
                return false;
            var ids = new HashSet<int>();
            foreach (var button in buttons)
                if (button == null || !button.HasLight || button.ButtonId < 1 || !ids.Add(button.ButtonId)) return false;
            foreach (int id in correctOrder) if (!ids.Contains(id)) return false;
            return true;
        }

        public void PressButton(LightSequenceButton button)
        {
            if (!CanAcceptInput || input == null || !input.CanInteract || button == null ||
                !button.CanInteract || System.Array.IndexOf(buttons, button) < 0) return;
            switch (puzzle.Press(button.ButtonId))
            {
                case SequencePressResult.Correct:
                    button.SetCorrect();
                    break;
                case SequencePressResult.Completed:
                    button.SetCorrect();
                    StateChanged?.Invoke();
                    SoundManager.Play(GameSound.PuzzleCompleted);
                    onCompleted.Invoke();
                    break;
                case SequencePressResult.Incorrect:
                    TurnOffButtons();
                    showingError = true;
                    errorRoutine = StartCoroutine(ShowError(button));
                    break;
            }
        }

        private IEnumerator ShowError(LightSequenceButton button)
        {
            yield return button.FlashError();
            showingError = false;
            errorRoutine = null;
        }

        private void CancelError()
        {
            if (errorRoutine != null) StopCoroutine(errorRoutine);
            errorRoutine = null;
            if (showingError) TurnOffButtons();
            showingError = false;
        }

        private void TurnOffButtons()
        {
            if (buttons == null) return;
            foreach (var button in buttons) if (button != null) button.SetOff();
        }
    }
}
