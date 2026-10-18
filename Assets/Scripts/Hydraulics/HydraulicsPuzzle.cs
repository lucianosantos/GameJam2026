using System;
using System.Collections.Generic;

namespace GameJam2026.Hydraulics
{
    // Bits and rotations follow North, East, South, West (clockwise).
    public sealed class HydraulicsPuzzle
    {
        public const int Columns = 6;
        public const int Rows = 6;
        public const int North = 1, East = 2, South = 4, West = 8;
        private readonly int[] shapes = new int[Columns * Rows];
        private readonly int[] rotations = new int[Columns * Rows];
        private readonly bool[] wet = new bool[Columns * Rows];
        public int Count => shapes.Length;
        public bool IsSolved { get; private set; }
        public bool IsWet(int index) => wet[index];
        public bool IsLocked(int index) => index == 0 || index == Count - 1;
        public bool IsElbow(int index) => shapes[index] == (East | South);
        public int Rotation(int index) => rotations[index];
        public int Connections(int index) => RotateMask(shapes[index], rotations[index]);

        public HydraulicsPuzzle(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < Count; i++)
                shapes[i] = random.Next(2) == 0 ? North | South : East | South;

            // Authored route, indexed from the top-left. Only rotations are shuffled.
            int[] route = { 0, 1, 2, 3, 4, 10, 16, 15, 14, 13, 19, 20, 21, 22,
                28, 27, 26, 25, 31, 32, 33, 34, 35 };
            for (int i = 0; i < route.Length; i++)
            {
                int index = route[i];
                int mask = (i == 0 ? West : Direction(index, route[i - 1])) |
                           (i == route.Length - 1 ? East : Direction(index, route[i + 1]));
                shapes[index] = mask == (North | South) || mask == (East | West)
                    ? North | South : East | South;
                for (int rotation = 0; rotation < 4; rotation++)
                    if (RotateMask(shapes[index], rotation) == mask)
                    {
                        rotations[index] = rotation;
                        break;
                    }
            }
            Evaluate();
        }

        public void Shuffle(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < Count; i++)
                if (!IsLocked(i)) rotations[i] = random.Next(4);
            Evaluate();
            // A new round must require interaction. The first interior tile blocks the inlet.
            if (IsSolved)
            {
                rotations[1] = 0;
                Evaluate();
            }
        }

        public bool Rotate(int index)
        {
            if (index < 0 || index >= Count || IsSolved || IsLocked(index)) return false;
            rotations[index] = (rotations[index] + 1) % 4;
            Evaluate();
            return true;
        }

        public static int RotateMask(int mask, int quarterTurns)
        {
            int turns = ((quarterTurns % 4) + 4) % 4;
            for (int i = 0; i < turns; i++) mask = ((mask << 1) & 15) | ((mask >> 3) & 1);
            return mask;
        }

        private static int Direction(int from, int to)
        {
            if (to == from - Columns) return North;
            if (to == from + Columns) return South;
            return to > from ? East : West;
        }

        private void Evaluate()
        {
            Array.Clear(wet, 0, wet.Length);
            var queue = new Queue<int>();
            if ((Connections(0) & West) != 0) { wet[0] = true; queue.Enqueue(0); }
            int[] offsets = { -Columns, 1, Columns, -1 };
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int mask = Connections(current);
                for (int direction = 0; direction < 4; direction++)
                {
                    if ((mask & (1 << direction)) == 0) continue;
                    if (direction == 1 && current % Columns == Columns - 1 ||
                        direction == 3 && current % Columns == 0) continue;
                    int next = current + offsets[direction];
                    if (next < 0 || next >= Count || wet[next]) continue;
                    if ((Connections(next) & (1 << ((direction + 2) % 4))) == 0) continue;
                    wet[next] = true;
                    queue.Enqueue(next);
                }
            }
            IsSolved = wet[Count - 1] && (Connections(Count - 1) & East) != 0;
        }
    }
}
