using System;
using GameJam2026.Wires;

public static class WiresPuzzleChecks
{
    private static readonly string[] Ports = { "z", "z", "x", "infinito", "m", "musica", "e", "p" };
    private static readonly WiresPair[] Pairs = { new WiresPair("p", "z"), new WiresPair("musica", "m"),
        new WiresPair("z", "infinito"), new WiresPair("x", "e") };
    private static int visited, completed;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static WiresPuzzle NewPuzzle() => new WiresPuzzle(Ports, Pairs);

    public static string Run()
    {
        visited = completed = 0;
        Enumerate(new int[8], new bool[8], 0);
        Check(visited == 40320 && completed == 768, "All socket permutations must have exactly 768 equivalent solutions.");
        var puzzle = NewPuzzle();
        Check(puzzle.TryConnect(0, 0) && puzzle.TryConnect(1, 1), "Z-Z must be allowed.");
        Check(!puzzle.TryConnect(2, 0), "Occupied socket must not replace another tip.");
        Check(puzzle.GetSocket(0) == 0 && puzzle.GetSocket(2) == -1, "Rejected attempt must preserve state.");
        Check(!puzzle.TryConnect(-1, 4) && !puzzle.TryConnect(2, 99), "Invalid indices rejected.");
        for (int i = 2; i < 8; i++) Check(puzzle.TryConnect(i, i), "Wrong complete arrangement accepted physically.");
        Check(!puzzle.IsCompleted, "Z-Z must not solve the puzzle.");
        for (int i = 0; i < 8; i++) Check(puzzle.Disconnect(i), "Wrong arrangement stays editable.");
        int[] solution = { 7, 0, 5, 4, 1, 3, 2, 6 };
        for (int i = 0; i < 8; i++) Check(puzzle.TryConnect(i, solution[i]), "Correction must succeed.");
        Check(puzzle.IsCompleted, "Corrected arrangement completes.");
        for (int i = 0; i < 8; i++)
        {
            Check(!puzzle.Disconnect(i) && !puzzle.TryConnect(i, i), "Completion locks every endpoint.");
            Check(puzzle.GetSocket(i) == solution[i], "Completed connections preserved.");
        }
        string[] copy = (string[])Ports.Clone(); WiresPair[] pairsCopy = (WiresPair[])Pairs.Clone();
        puzzle = new WiresPuzzle(copy, pairsCopy); copy[0] = "bad"; pairsCopy[0] = new WiresPair("bad", "bad");
        for (int i = 0; i < 8; i++) puzzle.TryConnect(i, solution[i]);
        Check(puzzle.IsCompleted, "Caller mutation must not alter configured solution.");
        return "PASS: 40320 socket permutations, 768 equivalent solutions, Z-Z, capacity, invalid indices, corrections, completion lock and copied configuration.";
    }
    private static void Enumerate(int[] permutation, bool[] used, int at)
    {
        if (at == 8)
        {
            var puzzle = NewPuzzle();
            for (int i = 0; i < 8; i++)
            { Check(puzzle.TryConnect(i, permutation[i]), "Every free physical socket must accept a tip."); if (i < 7) Check(!puzzle.IsCompleted, "Incomplete puzzle cannot complete."); }
            visited++; if (puzzle.IsCompleted) completed++;
            return;
        }
        for (int socket = 0; socket < 8; socket++)
        { if (used[socket]) continue; used[socket] = true; permutation[at] = socket; Enumerate(permutation, used, at + 1); used[socket] = false; }
    }
}
