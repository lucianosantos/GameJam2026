namespace GameJam2026.Hydraulics
{
    public static class HydraulicsPuzzleChecks
    {
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new System.Exception(message);
        }

        // Independent reachability oracle: compare pairs of grid cells, without index offsets.
        private static void CheckWater(HydraulicsPuzzle puzzle)
        {
            var expected = new bool[puzzle.Count];
            expected[0] = (puzzle.Connections(0) & 8) != 0;
            bool changed;
            do
            {
                changed = false;
                for (int a = 0; a < puzzle.Count; a++)
                for (int b = 0; b < puzzle.Count; b++)
                {
                    if (!expected[a] || expected[b]) continue;
                    int dx = b % HydraulicsPuzzle.Columns - a % HydraulicsPuzzle.Columns;
                    int dy = b / HydraulicsPuzzle.Columns - a / HydraulicsPuzzle.Columns;
                    int exit = 0, entry = 0;
                    if (dx == 1 && dy == 0) { exit = 2; entry = 8; }
                    if (dx == -1 && dy == 0) { exit = 8; entry = 2; }
                    if (dx == 0 && dy == 1) { exit = 4; entry = 1; }
                    if (dx == 0 && dy == -1) { exit = 1; entry = 4; }
                    if ((puzzle.Connections(a) & exit) == 0 ||
                        (puzzle.Connections(b) & entry) == 0) continue;
                    expected[b] = true;
                    changed = true;
                }
            } while (changed);
            for (int i = 0; i < puzzle.Count; i++)
                Check(expected[i] == puzzle.IsWet(i), "Incorrect water reachability at " + i);
            int outlet = puzzle.Count - 1;
            Check(puzzle.IsSolved == (expected[outlet] && (puzzle.Connections(outlet) & 2) != 0),
                "Incorrect completion state");
        }

        public static string Run()
        {
            Check(HydraulicsPuzzle.RotateMask(1, 1) == 2, "Clockwise rotation");
            Check(HydraulicsPuzzle.RotateMask(8, 1) == 1, "Rotation wraps");
            Check(HydraulicsPuzzle.RotateMask(6, 4) == 6, "Four rotations restore shape");
            for (int seed = 0; seed < 1000; seed++)
            {
                var puzzle = new HydraulicsPuzzle(seed);
                Check(puzzle.Count == 36, "Board must have six columns and six rows");
                Check(puzzle.IsSolved, "Authored path must be solvable");
                for (int row = 0; row < HydraulicsPuzzle.Rows; row++)
                {
                    bool visited = false;
                    for (int column = 0; column < HydraulicsPuzzle.Columns; column++)
                        visited |= puzzle.IsWet(row * HydraulicsPuzzle.Columns + column);
                    Check(visited, "Authored path must use every row");
                }
                var solution = new int[puzzle.Count];
                for (int i = 0; i < puzzle.Count; i++) solution[i] = puzzle.Rotation(i);
                puzzle.Shuffle(seed);
                Check(!puzzle.IsSolved, "Shuffle must start unsolved");
                Check(!puzzle.Rotate(0) && !puzzle.Rotate(puzzle.Count - 1), "Terminals are fixed");
                Check(!puzzle.Rotate(-1) && !puzzle.Rotate(puzzle.Count), "Invalid indices are ignored");
                var random = new System.Random(seed);
                for (int turn = 0; turn < 20; turn++)
                {
                    puzzle.Rotate(random.Next(puzzle.Count));
                    CheckWater(puzzle);
                }
                for (int i = 1; i < puzzle.Count - 1 && !puzzle.IsSolved; i++)
                    for (int turn = 0; turn < 4 && !puzzle.IsSolved &&
                        puzzle.Rotation(i) != solution[i]; turn++) puzzle.Rotate(i);
                Check(puzzle.IsSolved, "Restoring rotations must complete the board");
                Check(!puzzle.Rotate(1), "Completed board is frozen");
            }
            var feedback = new HydraulicsPuzzle(7);
            feedback.Shuffle(7);
            while (feedback.Rotation(1) != 0) feedback.Rotate(1);
            Check(!feedback.IsWet(1), "Mismatched ports must block water");
            feedback.Rotate(1);
            Check(feedback.IsWet(1), "Reciprocal ports must carry water");
            feedback.Rotate(1);
            Check(!feedback.IsWet(1), "Water must recede after disconnecting");
            return "PASS: 1000 seeds, solvability, shuffle, rotations, reciprocal connections, borders, water reset and completion lock.";
        }
    }
}
