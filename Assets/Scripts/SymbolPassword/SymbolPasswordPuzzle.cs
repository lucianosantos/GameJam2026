using System;
using System.Collections.Generic;

namespace GameJam2026.SymbolPassword
{
    public sealed class SymbolPasswordPuzzle
    {
        private readonly int[] solution;
        private readonly List<int> entered = new List<int>();
        public IReadOnlyList<int> Entered => entered;
        public bool IsCompleted { get; private set; }
        public int Capacity => solution.Length;

        public SymbolPasswordPuzzle(int[] solution)
        {
            if (solution == null || solution.Length == 0) throw new ArgumentException("A password is required.");
            foreach (int digit in solution)
                if (digit < 0 || digit > 9) throw new ArgumentException("Digits must be between zero and nine.");
            this.solution = (int[])solution.Clone();
        }

        public bool Enter(int digit)
        {
            if (IsCompleted || entered.Count == Capacity || digit < 0 || digit > 9) return false;
            entered.Add(digit);
            return true;
        }

        public void Clear() { if (!IsCompleted) entered.Clear(); }

        public bool Submit()
        {
            if (IsCompleted || entered.Count != Capacity) return false;
            for (int i = 0; i < solution.Length; i++) if (entered[i] != solution[i]) return false;
            IsCompleted = true;
            return true;
        }

        public void Restart() { IsCompleted = false; entered.Clear(); }
    }
}
