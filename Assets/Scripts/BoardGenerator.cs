using System;
using System.Collections.Generic;

public class BoardGenerator
{
    readonly int N;
    readonly int minSize, maxSize;
    readonly Random rng;

    // results
    public int[] Marks;
    public int[] RegionOf;
    public int SolverRuns;

    static readonly int[] dR = {-1, 1, 0, 0};
    static readonly int[] dC = {0, 0, -1, 1};

    // conflicts[cell] = every cell ruled out by a mark on 'cell' (row, column, 8 neighbors)
    // depends only on N, so it's built once
    readonly int[][] conflicts;

    // working state, allocated once and reused
    readonly int[] marks;
    readonly bool[] colUsed;
    readonly bool[] isOrigMark;
    readonly int[] regionOf;
    readonly int[] regionSize;
    readonly List<int>[] frontier;
    readonly List<int>[] regionCells;
    readonly int[] blocked;      // blocked[cell] = number of placed marks ruling this cell out
    readonly int[] availCount;   // availCount[region] = cells in region with blocked == 0
    readonly bool[] regionDone;
    readonly int[] placed;       // placed[depth] = cell of the mark placed at that depth
    readonly int[] altCells;     // a full solution that differes from the intended one
    bool altFound;
    readonly bool[] visited;
    readonly Stack<int> stack = new Stack<int>();

    readonly LogicSolver logic;
    public GradeResult Grade;

    public BoardGenerator(int n, int seed, int minRegionSize, int maxRegionSize)
    {
        N = n;
        minSize = minRegionSize;
        maxSize = maxRegionSize;

        if (seed == 0)
        {
            rng = new Random();
        }
        else
        {
            rng = new Random(seed);
        }

        int cells = n * n;
        conflicts = new int[cells][];
        var tmp = new List<int>();
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                tmp.Clear();
                for (int r2 = 0; r2 < n; r2++)
                {
                    for (int c2 = 0; c2 < n; c2++)
                    {
                        if (r2 == r || c2 == c || (Math.Abs(r2 - r) <= 1 && Math.Abs(c2 - c) <= 1))
                        {
                            tmp.Add(r2 * n + c2);
                        }
                    }
                }
                conflicts[r * n + c] = tmp.ToArray();
            }
        }

        marks = new int[n];
        colUsed = new bool[n];
        isOrigMark = new bool[cells];
        regionOf = new int[cells];
        regionSize = new int[n];
        frontier = new List<int>[n];
        regionCells = new List<int>[n];
        for (int i = 0; i < n; i++)
        {
            frontier[i] = new List<int>();
            regionCells[i] = new List<int>();
        }
        blocked = new int[cells];
        availCount = new int[n];
        regionDone = new bool[n];
        placed = new int[n];
        altCells = new int[n];
        visited = new bool[cells];

        logic = new LogicSolver(n);
    }

    // 
    // TOP LEVEL
    //
    public bool Generate(int maxLayouts = 50, int regionTries = 20, int maxRepairs = 200, Func<GradeResult, bool> accept = null)
    {
        // cant generate a board less than size 4
        if (N < 4)
        {
            return false;
        }
        
        for (int layout = 0; layout < maxLayouts; layout++)
        {
            if (!PlaceMarks())
            {
                continue;
            }

            for (int t = 0; t < regionTries; t++)
            {
                GrowRegions();
                if (!SizesOk())
                {
                    continue;
                }

                if (Repair(maxRepairs))
                {
                    GradeResult g = logic.Grade(regionOf);
                    if (accept != null && !accept(g))
                    {
                        continue;
                    }
                    Marks = (int[])marks.Clone();
                    RegionOf = (int[])regionOf.Clone();
                    Grade = g;
                    return true;
                }
            }
        }
        return false;
    }

    // 
    // Lesson 1: Marks
    //
    bool PlaceMarks()
    {
        Array.Fill(marks, -1);
        Array.Clear(colUsed, 0, N);
        if (!PlaceRow(0))
        {
            return false;
        }

        Array.Clear(isOrigMark, 0, isOrigMark.Length);
        for (int r = 0; r < N; r++)
        {
            isOrigMark[r * N + marks[r]] = true;
        }
        return true;
    }

    bool PlaceRow(int row)
    {
        if (row == N)
        {
            return true;
        }

        int[] cand = new int[N];
        int count = 0;
        for (int c = 0; c < N; c++)
        {
            if (colUsed[c])
            {
                continue;
            }
            if (row > 0 && Math.Abs(c - marks[row - 1]) <= 1)
            {
                continue;
            }
            cand[count++] = c;
        }

        for (int i = count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cand[i], cand[j]) = (cand[j], cand[i]);
        }

        for (int i = 0; i < count; i++)
        {
            marks[row] = cand[i];
            colUsed[cand[i]] = true;
            if (PlaceRow(row + 1))
            {
                return true;
            }
            colUsed[cand[i]] = false;
            marks[row] = -1;
        }
        return false;
    }

    //
    // Lesson 2: regions, with persistent (lazy) frontiers
    //
    void GrowRegions()
    {
        Array.Fill(regionOf, -1);
        for (int i = 0; i < N; i++)
        {
            frontier[i].Clear();
            regionSize[i] = 1;
            regionOf[i * N + marks[i]] = i;
        }
        for (int i = 0; i < N; i++)
        {
            PushNeighbors(i * N + marks[i], i);
        }

        int unclaimed = N * N - N;
        while (unclaimed > 0)
        {
            int reg = rng.Next(N);
            List<int> f = frontier[reg];

            while (f.Count > 0)
            {
                int idx = rng.Next(f.Count);
                int cell = f[idx];
                f[idx] = f[f.Count - 1];  // swap-remove
                f.RemoveAt(f.Count -1);

                if (regionOf[cell] != -1)
                {
                    continue;            // stale: someone else took it
                }

                regionOf[cell] = reg;
                regionSize[reg]++;
                unclaimed--;
                PushNeighbors(cell, reg);
                break;
            }
        }
    }

    void PushNeighbors(int cell, int reg)
    {
        int r = cell / N, c = cell % N;
        for (int d = 0; d < 4; d++)
        {
            int nr = r + dR[d], nc = c + dC[d];
            if (nr < 0 || nr >= N || nc < 0 || nc >= N)
            {
                continue;   
            }
            int n = nr * N + nc;
            if (regionOf[n] == -1)
            {
                frontier[reg].Add(n);
            }
        }
    }

    bool SizesOk()
    {
        for (int i = 0; i < N; i++)
        {
            if (regionSize[i] < minSize || regionSize[i] > maxSize)
            {
                return false;
            }
        }
        return true;
    }

    // 
    // Lesson 3
    // 
    void BuildSolverState()
    {
        for (int i = 0; i < N; i++)
        {
            regionCells[i].Clear();
        }
        for (int cell = 0; cell < N * N; cell++)
        {
            regionCells[regionOf[cell]].Add(cell);
        }

        for (int i = 0; i < N; i++)
        {
            regionSize[i] = regionCells[i].Count;
            availCount[i] = regionSize[i];
            regionDone[i] = false;
        }
        Array.Clear(blocked, 0, blocked.Length);
    }

    void Place(int cell)
    {
        regionDone[regionOf[cell]] = true;
        int[] list = conflicts[cell];
        for (int i = 0; i < list.Length; i++)
        {
            int x = list[i];
            if (blocked[x]++ == 0)
            {
                availCount[regionOf[x]]--;
            }
        }
    }

    void Remove(int cell)
    {
        int[] list = conflicts[cell];
        for (int i = 0; i < list.Length; i++)
        {
            int x = list[i];
            if (--blocked[x] == 0)
            {
                availCount[regionOf[x]]++;
            }
        }
        regionDone[regionOf[cell]] = false;
    }

    int Solve(int depth, int limit)
    {
        if (depth == N)
        {
            // remember a solution that isn't the intended one
            if (!altFound)
            {
                for (int k = 0; k < N; k++)
                {
                    if (!isOrigMark[placed[k]])
                    {
                        altFound = true;
                        Array.Copy(placed, altCells, N);
                        break;
                    }
                }
            }
            return 1;
        }

        // most constrained unfinished region; any region with 0 options is a dead end
        int best = -1, bestCount = int.MaxValue;
        for (int reg = 0; reg < N; reg++)
        {
            if (regionDone[reg])
            {
                continue;
            }
            int a = availCount[reg];
            if (a == 0 )
            {
                return 0;
            }
            if (a < bestCount)
            {
                bestCount = a;
                best = reg;
            }
        }

        int total = 0;
        List<int> cells = regionCells[best];
        for (int i = 0; i < cells.Count; i++)
        {
            int cell = cells[i];
            if (blocked[cell] != 0)
            {
                continue;
            }

            placed[depth] = cell;
            Place(cell);
            total += Solve(depth + 1, limit - total);
            Remove(cell);

            if (total >= limit)
            {
                break;
            }
        }
        return total;
    }

    //
    // Repair: use an extra solution to fix board
    //
    bool Repair(int maxRepairs)
    {
        for (int i = 0; i < maxRepairs; i++)
        {
            BuildSolverState();
            altFound = false;
            SolverRuns++;
            int count = Solve(0, 2);

            // if unique
            if (count == 1) 
            {
                return true;
            }
            // should not happen; signals a bug
            if (count == 0 || !altFound)
            {
                return false;
            }

            // stuck, caller regrows
            if (!TryFixOneCell())
            {
                return false;
            }
        }
        return false;
    }

    // Move one cell of the alternate solution into a neighboring region
    // That region then holds two of the alternates marks, so the alternate is dead,
    // while the intened solution is untouched (the moved cell is never one of its marks).
    bool TryFixOneCell()
    {
        var targets = new List<int>(N);
        for (int k = 0; k < N; k++)
        {
            if (!isOrigMark[altCells[k]])
            {
                targets.Add(altCells[k]);
            }
        }

        for (int i = targets.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (targets[i], targets[j]) = (targets[j], targets[i]);
        }

        foreach (int cell in targets)
        {
            int a = regionOf[cell];
            if (regionSize[a] - 1 < minSize)
            {
                continue;
            }
            if (!StaysConnected(a, cell))
            {
                continue;
            }

            int r = cell / N, c = cell % N;
            int start = rng.Next(4);
            for (int d0 = 0; d0 < 4; d0++)
            {
                int d = (d0 + start) & 3;
                int nr = r + dR[d], nc = c + dC[d];
                if (nr < 0 || nr >= N || nc < 0 || nc >= N)
                {
                    continue;
                }

                int b = regionOf[nr * N + nc];
                if (b == a)
                {
                    continue;
                }
                if (regionSize[b] + 1 > maxSize)
                {
                    continue;
                }

                regionOf[cell] = b;  // sizes are rebuilt at the top of the next repair
                return true;
            }
        }
        return false;
    }

    // Would region 'region' stay in one piece if 'removed' were taken out of it?
    bool StaysConnected(int region, int removed)
    {
        int start = region * N + marks[region];
        Array.Clear(visited, 0, visited.Length);
        visited[removed] = true;
        visited[start] = true;
        stack.Clear();
        stack.Push(start);

        int reached = 1;
        while (stack.Count > 0)
        {
            int cur = stack.Pop();
            int r = cur / N, c = cur % N;
            for (int d = 0; d < 4;  d++)
            {
                int nr = r + dR[d], nc = c + dC[d];
                if (nr < 0 || nr >= N || nc < 0 || nc >= N)
                {
                    continue;
                }
                int n = nr * N + nc;
                if (visited[n] || regionOf[n] != region)
                {
                    continue;
                }
                visited[n] = true;
                reached++;
                stack.Push(n);
            }
        }
        return reached == regionSize[region] - 1;
    }
}