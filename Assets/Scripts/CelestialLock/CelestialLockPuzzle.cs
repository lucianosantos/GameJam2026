using System;

namespace GameJam2026.CelestialLock
{
    public enum CelestialSymbol { Moon, Star, Sun, Saturn }

    // No Unity dependency. Symbols may repeat; only explicit submission checks the order.
    public sealed class CelestialLockPuzzle
    {
        private readonly int[] solution;
        private readonly int[] initial;
        private readonly int[] selected = new int[4];
        public bool IsCompleted { get; private set; }
        public int SymbolAt(int index) => selected[index];

        public CelestialLockPuzzle(int[] solution, int[] initial)
        {
            Validate(solution); Validate(initial);
            this.solution = (int[])solution.Clone();
            this.initial = (int[])initial.Clone();
            Restart();
        }
        private static void Validate(int[] values)
        {
            if (values == null || values.Length != 4) throw new ArgumentException("Exactly four symbols are required.");
            foreach (int symbol in values)
                if (symbol < 0 || symbol > 3) throw new ArgumentOutOfRangeException(nameof(values));
        }
        public bool Advance(int index)
        {
            if (IsCompleted || index < 0 || index >= 4) return false;
            selected[index] = (selected[index] + 1) % 4;
            return true;
        }
        public bool Submit()
        {
            if (IsCompleted) return false;
            for (int i = 0; i < 4; i++) if (selected[i] != solution[i]) return false;
            IsCompleted = true;
            return true;
        }
        public void Restart()
        {
            Array.Copy(initial, selected, 4);
            IsCompleted = false;
        }
    }
}
