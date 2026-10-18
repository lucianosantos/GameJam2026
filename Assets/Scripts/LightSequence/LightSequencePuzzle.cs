using System;

namespace GameJam2026.LightSequence
{
    public enum SequencePressResult { Ignored, Correct, Incorrect, Completed }

    public sealed class LightSequencePuzzle
    {
        private readonly int[] order;
        public int Progress { get; private set; }
        public bool IsCompleted => Progress == order.Length;

        public LightSequencePuzzle(int[] correctOrder)
        {
            if (correctOrder == null || correctOrder.Length == 0)
                throw new ArgumentException("The sequence must contain at least one button.", nameof(correctOrder));
            order = (int[])correctOrder.Clone();
        }

        public SequencePressResult Press(int buttonId)
        {
            if (IsCompleted) return SequencePressResult.Ignored;
            if (order[Progress] != buttonId)
            {
                Progress = 0;
                return SequencePressResult.Incorrect;
            }
            Progress++;
            return IsCompleted ? SequencePressResult.Completed : SequencePressResult.Correct;
        }

        public void Restart() => Progress = 0;
    }
}
