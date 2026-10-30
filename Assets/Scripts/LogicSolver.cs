using System;
using System.Collections.Generic;

public class GradeResult
{
    public bool Solved;            // false = stuck, needs more than these techniques
    public int HardestTier;        // highest tier used (1-4)
    public int[] Steps = new int[5]; // Steps[t] = times tier t was needed (index 0 unused)
    public int Score;              // weighted total, for finer ranking
}

public class LogicSolver
{
    readonly int N;
    readonly int[][] conflicts;     // row, column, and 8 neighbors of each cell
    int[] regionOf;                 // set in Grade()
    readonly bool[] cand;           // cand[cell] = a cat could still go here
    readonly bool[] regDone, rowDone, colDone;
    readonly List<int>[] regionCells;
    int placedCount;

    // scratch arrays, allocated once
    readonly int[] regRows, regCols, rowRegs, colRegs;   // bitmasks
    readonly int[] regCount, rowCount, colCount;
    readonly int[] stamp;
    int stampId;
    readonly List<int> removed = new List<int>();

    public LogicSolver(int n)
    {
        N = n;
        int cells = n * n;

        conflicts = new int[cells][];
        var tmp = new List<int>();
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
            {
                tmp.Clear();
                for (int r2 = 0; r2 < n; r2++)
                    for (int c2 = 0; c2 < n; c2++)
                        if (r2 == r || c2 == c ||
                            (Math.Abs(r2 - r) <= 1 && Math.Abs(c2 - c) <= 1))
                            tmp.Add(r2 * n + c2);
                conflicts[r * n + c] = tmp.ToArray();
            }

        cand = new bool[cells];
        stamp = new int[cells];
        regDone = new bool[n]; rowDone = new bool[n]; colDone = new bool[n];
        regionCells = new List<int>[n];
        for (int i = 0; i < n; i++) regionCells[i] = new List<int>();
        regRows = new int[n]; regCols = new int[n];
        rowRegs = new int[n]; colRegs = new int[n];
        regCount = new int[n]; rowCount = new int[n]; colCount = new int[n];
    }

    // regions[row * N + col] = region id
    public GradeResult Grade(int[] regions)
    {
        regionOf = regions;
        for (int i = 0; i < N; i++) regionCells[i].Clear();
        for (int cell = 0; cell < N * N; cell++) regionCells[regionOf[cell]].Add(cell);

        Array.Fill(cand, true);
        Array.Clear(regDone, 0, N);
        Array.Clear(rowDone, 0, N);
        Array.Clear(colDone, 0, N);
        placedCount = 0;

        var result = new GradeResult();
        int maxGroup = Math.Max(2, N / 2);

        while (placedCount < N)
        {
            int tier;
            if (Singles()) tier = 1;
            else if (Groups(1)) tier = 2;
            else if (GroupsUpTo(maxGroup)) tier = 3;
            else if (LookAhead()) tier = 4;
            else { result.Solved = false; break; }

            result.Steps[tier]++;
            if (tier > result.HardestTier) result.HardestTier = tier;

            if (placedCount == N) result.Solved = true;
        }

        // weights are guesses: tune them after playing some boards
        result.Score = result.Steps[1] * 1 + result.Steps[2] * 3 +
                       result.Steps[3] * 8 + result.Steps[4] * 20;
        return result;
    }

    // ---------------- placing ----------------
    void Place(int cell)
    {
        int g = regionOf[cell];
        regDone[g] = true;
        rowDone[cell / N] = true;
        colDone[cell % N] = true;
        placedCount++;

        int[] list = conflicts[cell];
        for (int i = 0; i < list.Length; i++) cand[list[i]] = false;
        foreach (int x in regionCells[g]) cand[x] = false;
    }

    // ---------------- tier 1 ----------------
    bool Singles()
    {
        for (int g = 0; g < N; g++)
        {
            if (regDone[g]) continue;
            int found = -1, count = 0;
            foreach (int x in regionCells[g]) if (cand[x]) { found = x; count++; }
            if (count == 1) { Place(found); return true; }
        }
        for (int r = 0; r < N; r++)
        {
            if (rowDone[r]) continue;
            int found = -1, count = 0;
            for (int c = 0; c < N; c++) { int x = r * N + c; if (cand[x]) { found = x; count++; } }
            if (count == 1) { Place(found); return true; }
        }
        for (int c = 0; c < N; c++)
        {
            if (colDone[c]) continue;
            int found = -1, count = 0;
            for (int r = 0; r < N; r++) { int x = r * N + c; if (cand[x]) { found = x; count++; } }
            if (count == 1) { Place(found); return true; }
        }
        return false;
    }

    // ---------------- tiers 2 and 3 ----------------
    bool GroupsUpTo(int maxK)
    {
        for (int k = 2; k <= maxK; k++)
            if (Groups(k)) return true;
        return false;
    }

    // k = 1 is line-lock; k >= 2 is the grouped version.
    bool Groups(int k)
    {
        Array.Clear(regRows, 0, N); Array.Clear(regCols, 0, N);
        Array.Clear(rowRegs, 0, N); Array.Clear(colRegs, 0, N);

        for (int cell = 0; cell < N * N; cell++)
        {
            if (!cand[cell]) continue;
            int g = regionOf[cell], r = cell / N, c = cell % N;
            regRows[g] |= 1 << r; regCols[g] |= 1 << c;
            rowRegs[r] |= 1 << g; colRegs[c] |= 1 << g;
        }

        int undoneRegs = 0, undoneRows = 0, undoneCols = 0;
        for (int i = 0; i < N; i++)
        {
            if (!regDone[i]) undoneRegs |= 1 << i;
            if (!rowDone[i]) undoneRows |= 1 << i;
            if (!colDone[i]) undoneCols |= 1 << i;
        }

        for (int mask = 1; mask < (1 << N); mask++)
        {
            if (PopCount(mask) != k) continue;

            // k regions that fit inside k rows (or k columns)
            if ((mask & ~undoneRegs) == 0)
            {
                int rows = 0, cols = 0;
                for (int g = 0; g < N; g++)
                    if (((mask >> g) & 1) != 0) { rows |= regRows[g]; cols |= regCols[g]; }

                if (PopCount(rows) == k && ClearLinesExceptRegions(rows, mask, true)) return true;
                if (PopCount(cols) == k && ClearLinesExceptRegions(cols, mask, false)) return true;
            }

            // k rows (or columns) that only touch k regions
            if ((mask & ~undoneRows) == 0)
            {
                int regs = 0;
                for (int r = 0; r < N; r++)
                    if (((mask >> r) & 1) != 0) regs |= rowRegs[r];
                if (PopCount(regs) == k && ClearRegionsExceptLines(regs, mask, true)) return true;
            }
            if ((mask & ~undoneCols) == 0)
            {
                int regs = 0;
                for (int c = 0; c < N; c++)
                    if (((mask >> c) & 1) != 0) regs |= colRegs[c];
                if (PopCount(regs) == k && ClearRegionsExceptLines(regs, mask, false)) return true;
            }
        }
        return false;
    }

    // In the given lines, remove candidates that belong to regions outside regionMask.
    bool ClearLinesExceptRegions(int lines, int regionMask, bool isRow)
    {
        bool changed = false;
        for (int L = 0; L < N; L++)
        {
            if (((lines >> L) & 1) == 0) continue;
            for (int p = 0; p < N; p++)
            {
                int cell = isRow ? L * N + p : p * N + L;
                if (cand[cell] && ((regionMask >> regionOf[cell]) & 1) == 0)
                {
                    cand[cell] = false;
                    changed = true;
                }
            }
        }
        return changed;
    }

    // For the given regions, remove candidates that sit outside the lines in lineMask.
    bool ClearRegionsExceptLines(int regs, int lineMask, bool isRow)
    {
        bool changed = false;
        for (int g = 0; g < N; g++)
        {
            if (((regs >> g) & 1) == 0) continue;
            foreach (int cell in regionCells[g])
            {
                if (!cand[cell]) continue;
                int line = isRow ? cell / N : cell % N;
                if (((lineMask >> line) & 1) == 0)
                {
                    cand[cell] = false;
                    changed = true;
                }
            }
        }
        return changed;
    }

    // ---------------- tier 4 ----------------
    bool LookAhead()
    {
        Array.Clear(regCount, 0, N);
        Array.Clear(rowCount, 0, N);
        Array.Clear(colCount, 0, N);
        for (int cell = 0; cell < N * N; cell++)
        {
            if (!cand[cell]) continue;
            regCount[regionOf[cell]]++;
            rowCount[cell / N]++;
            colCount[cell % N]++;
        }

        for (int cell = 0; cell < N * N; cell++)
        {
            if (!cand[cell]) continue;
            if (WouldContradict(cell))
            {
                cand[cell] = false;
                return true;     // one elimination per step keeps the step count honest
            }
        }
        return false;
    }

    // If a cat went here, would some region, row, or column be left with no options?
    bool WouldContradict(int cell)
    {
        int g0 = regionOf[cell], r0 = cell / N, c0 = cell % N;
        stampId++;
        removed.Clear();

        int[] list = conflicts[cell];
        for (int i = 0; i < list.Length; i++) MarkRemoved(list[i]);
        foreach (int x in regionCells[g0]) MarkRemoved(x);

        for (int i = 0; i < removed.Count; i++)
        {
            int x = removed[i];
            regCount[regionOf[x]]--; rowCount[x / N]--; colCount[x % N]--;
        }

        bool bad = false;
        for (int i = 0; i < N && !bad; i++)
        {
            if (!regDone[i] && i != g0 && regCount[i] == 0) bad = true;
            else if (!rowDone[i] && i != r0 && rowCount[i] == 0) bad = true;
            else if (!colDone[i] && i != c0 && colCount[i] == 0) bad = true;
        }

        for (int i = 0; i < removed.Count; i++)
        {
            int x = removed[i];
            regCount[regionOf[x]]++; rowCount[x / N]++; colCount[x % N]++;
        }
        return bad;
    }

    void MarkRemoved(int x)
    {
        if (cand[x] && stamp[x] != stampId)
        {
            stamp[x] = stampId;
            removed.Add(x);
        }
    }

    static int PopCount(int v)
    {
        int c = 0;
        while (v != 0) { v &= v - 1; c++; }
        return c;
    }
}