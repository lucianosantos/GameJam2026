using System;

namespace GameJam2026.Levers
{
    public sealed class LeversPuzzle
    {
        private readonly int[] solution;
        private readonly int[] positions;
        public bool IsCompleted { get; private set; }
        public int Count => positions.Length;
        public int GetPosition(int index) => positions[index];

        public LeversPuzzle(int[] correctPositions)
        {
            if (correctPositions == null || correctPositions.Length != 3)
                throw new ArgumentException("Configure exactly three levers.", nameof(correctPositions));
            solution = (int[])correctPositions.Clone();
            foreach (int position in solution)
                if (position < 1 || position > 4)
                    throw new ArgumentException("Lever positions must be between 1 and 4.", nameof(correctPositions));
            positions = new int[solution.Length];
            Restart();
        }

        public bool SetPosition(int index, int position)
        {
            if (IsCompleted || index < 0 || index >= Count || position < 1 || position > 4 || positions[index] == position)
                return false;
            positions[index] = position;
            for (int i = 0; i < Count; i++)
                if (positions[i] != solution[i]) return true;
            IsCompleted = true;
            return true;
        }

        public void Restart()
        {
            // Start away from the solution even when all three targets are 1.
            int initial = Array.TrueForAll(solution, position => position == 1) ? 4 : 1;
            for (int i = 0; i < Count; i++) positions[i] = initial;
            IsCompleted = false;
        }
    }
}
