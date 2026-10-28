using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SolutionGenerator : MonoBehaviour
{
    [Min(4)]
    public int boardSize = 5;
    public GameObject columnPrefab;
    public GameObject tilePrefab;
    public Sprite catImage;

    // private List<List<bool>> solutionGrid = new List<List<bool>>();
    public List<int> solutionList = new List<int>();

    public List<Color> squareColors = new List<Color>();


    private List<bool> usedList = new List<bool>();
    private List<List<int>> colorRegions = new List<List<int>>(); // aka regionIds


    private static readonly int[] dRow = {-1, 1, 0, 0};
    private static readonly int[] dCol = {0, 0, -1, 1};

    // lesson 3 helpers
    List<(int row, int c)>[] regionCells = null;
    bool[] rowUsed;
    bool[] colUsed;
    bool[] regionDone;
    int[] solutionColInRow;

    // lesson 4 helpers
    public int minRegionSize = 3;
    public int maxRegionSize = 8; // try boardSize * 2

    int totalAttempts, rejectedBySize, rejectedbyUniqueness;
    bool boardGenerationSuccess = false;
    int[] regionSizeGlobal;


    void Awake()
    {
        // TODO: make this a singleton
    }

    void Start()
    { 
        
        StartCoroutine(GenerateTracker());
    }

    private IEnumerator GenerateTracker()
    {
        yield return StartCoroutine(GenerateRoutine(1));

        if (!boardGenerationSuccess)
        {
            Debug.LogError("Failed to generate a board");
            // TODO: fallback on pre-made board? use different seed?
        }
        else
        {
            GenerateGUI();
        }
    }

    private IEnumerator GenerateRoutine(int seed)
    {
        bool found = false;
        int attempts = 0;
        totalAttempts = 0;
        rejectedBySize = 0;
        rejectedbyUniqueness = 0;

        while (!found && attempts++ < 1000)
        {
            CreateSolutionList();
            if (attempts % 10 == 0)
            {
                yield return null;
            }

            for (int i = 0; i < 200 && !found; i++)
            {
                colorRegions = LoadRegionIds();

                if (!PassesQuickChecks())
                {
                    rejectedBySize++;
                    continue;
                }

                if (CountUnique(colorRegions) == 1)
                {
                    found = true;
                    boardGenerationSuccess = true;
                    // Debug.Log($"Repeated {i} times");
                }
                else
                {
                    rejectedbyUniqueness++;
                }
                totalAttempts++;
            }
        }
        Debug.Log($"Total Attempts: {totalAttempts}, Rejected by Size: {rejectedBySize}, Rejected by Uniq. {rejectedbyUniqueness}");
    }

    private void CreateSolutionList()
    {
        solutionList.Clear();
        usedList.Clear();
        for (int i = 0; i < boardSize; i++)
        {
            solutionList.Add(-1);
            usedList.Add(false);
        }
        PlaceRow(0);
    }

    private List<List<int>> LoadRegionIds()
    {
        List<List<int>> regionId = new List<List<int>>();
        List<int> regionSize = new List<int>();

        for (int i = 0; i < boardSize; i++)
        {
            List<int> newColumn = new List<int>();
            for (int j = 0; j < boardSize; j++)
            {
                newColumn.Add(-1);
            }
            regionId.Add(newColumn);
        }

        for (int i = 0; i < boardSize; i++)
        {
            regionId[i][solutionList[i]] = i;
            regionSize.Add(1);
        }
        
        int unclaimed = (boardSize * boardSize) - boardSize;
        // growth
        while (unclaimed > 0)
        {
            // pick region
            int region = UnityEngine.Random.Range(0, boardSize);

            List<(int row, int c)> frontier = GetColorFrontier(region, boardSize, regionId);
            // if empty, continue
            if (frontier.Count == 0)
            {
                continue;
            }

            (int row, int c) chosen = frontier[UnityEngine.Random.Range(0, frontier.Count)];

            regionId[chosen.row][chosen.c] = region; 
            regionSize[region] += 1;
            unclaimed -= 1; 
        }


        regionSizeGlobal = new int[boardSize];
        for (int i = 0; i < boardSize; i++)
        {
            regionSizeGlobal[i] = regionSize[i];
        }

        return regionId;

    }

    private List<(int row, int c)> GetColorFrontier(int region, int size, List<List<int>> regionId)
    {
        HashSet<(int row, int c)> found = new HashSet<(int row, int c)>();

        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                if (regionId[r][c] != region)
                {
                    continue;
                }
                for (int d = 0; d < 4; d++)
                {
                    int nr = r + dRow[d];
                    int nc = c + dCol[d];

                    if (InBounds(nr, nc, size) && regionId[nr][nc] == -1)
                    {
                        found.Add((nr, nc));
                    }
                }
            }   
        }

        return new List<(int row, int c)>(found);
    }

    private bool InBounds(int r, int c, int size)
    {
        return r >= 0 && r < size && c >= 0 && c < size;
    }

    private void GenerateGUI()
    {
        Transform rowManager = transform.GetChild(0);
        for (int i = 0; i < boardSize; i++)
        {
            Transform currentRow = Instantiate(columnPrefab, rowManager).transform;
            for (int j = 0; j < boardSize; j++)
            {
                Image tile = Instantiate(tilePrefab, currentRow).GetComponent<Image>();
                if (solutionList[i] == j)
                {
                    tile.sprite = catImage;
                }
                tile.color = squareColors[colorRegions[i][j]];
            }
        }
    }

    private bool PlaceRow(int row)
    {
        // if row == N: return success
        if (row == boardSize) return true;

        // candidates = all columns not yet used
        List<int> candidates = BuildCandidates(row);
        candidates.Shuffle();

        // for each c in candidates:
        foreach (int candidate in candidates)
        {
            // col[row] = c
            solutionList[row] = candidate;
            // mark c as used
            usedList[candidate] = true;

            if (PlaceRow(row + 1))
            {
                return true;
            }
            // unmark c
            usedList[candidate] = false;
            solutionList[row] = -1;
        } 
        // return fail
        return false;
    }

    private List<int> BuildCandidates(int row)
    {
        List<int> candidates = new List<int>();
        for (int i = 0; i < boardSize; i++)
        {
            if (usedList[i])
            {
                continue;
            }
            if (row > 0 && Mathf.Abs(i - solutionList[row-1]) <= 1)
            {
                continue;
            }
            candidates.Add(i);
        }
        return candidates;
    }

    private bool IsAvailable(int r, int c)
    {
        if (rowUsed[r]) return false;
        if (colUsed[c]) return false;
        if (regionDone[colorRegions[r][c]]) return false;

        if (r > 0 && solutionColInRow[r - 1] != -1 && Mathf.Abs(c - solutionColInRow[r - 1]) <= 1) return false;
        if (r < boardSize - 1 && solutionColInRow[r + 1] != -1 && Mathf.Abs(c - solutionColInRow[r + 1]) <= 1) return false;

        return true;
    }
    private int CountSolutions(int placedCount, int limit)
    {
        if (placedCount == boardSize)
        {
            return 1;
        }

        // find the most constrained unfinished region
        int bestRegion = -1;
        List<(int row, int c)> bestCandidates = null;

        for (int i = 0; i < boardSize; i++)
        {
            if (regionDone[i])
            {
                continue;
            }
            List<(int row, int col)> candidates = new List<(int row, int col)>();
            foreach ((int row, int c) cell in regionCells[i])
            {
                if (IsAvailable(cell.row, cell.c))
                {
                    candidates.Add(cell);
                }
            }

            if (candidates.Count == 0)
            {
                return 0; // no legal spot, dead end
            }
            if (bestRegion == -1 || candidates.Count < bestCandidates.Count)
            {
                bestRegion = i;
                bestCandidates = candidates;
            }
        }
        int total = 0;
        foreach ((int row, int c) cell in bestCandidates)
        {
            Place(cell.row, cell.c);
            total += CountSolutions(placedCount + 1, limit - total);
            Remove(cell.row, cell.c);

            if (total >= limit)
            {
                break;
            }
        }
        return total;
    }

    private void Place(int r, int c)
    {
        rowUsed[r] = true;
        colUsed[c] = true;
        regionDone[colorRegions[r][c]] = true;
        solutionColInRow[r] = c;
    }

    private void Remove(int r, int c)
    {
        rowUsed[r] = false;
        colUsed[c] = false;
        regionDone[colorRegions[r][c]] = false;
        solutionColInRow[r] = -1;
    }
    private int CountUnique(List<List<int>> regionId)
    {
        // reset rowUsed, colUsed, regionDone to false
        rowUsed = new bool[boardSize];
        colUsed = new bool[boardSize];
        regionDone = new bool[boardSize];
        ClearBoolArray(rowUsed, 0, boardSize);
        ClearBoolArray(colUsed, 0, boardSize);
        ClearBoolArray(regionDone, 0, boardSize);

        solutionColInRow = new int[boardSize];
        // reset catColInRow to all -1
        if (solutionColInRow == null)
        {
            for (int i = 0; i < boardSize; i++)
            {
                solutionColInRow[i] = -1;
            }
        }
        else
        {
            Array.Fill(solutionColInRow, -1);
        }

        regionCells = new List<(int row, int c)>[boardSize];
        for (int i = 0; i < boardSize; i++)
        {
            regionCells[i] = new List<(int row, int c)>();
        }
        for (int r = 0; r < boardSize; r++)
        {
            for (int c = 0; c < boardSize; c++)
            {
                regionCells[regionId[r][c]].Add((r, c));
            }
        }
        


        return CountSolutions(0, 2);
    }
    private void ClearBoolArray(bool[] array, int start, int end)
    {
        if (array == null)
        {
            for (int i = start; i < end; i++)
            {
                array[i] = false;
            }
        }
        else
        {
            Array.Clear(array, start, end);
        }
    }

    private bool PassesQuickChecks()
    {
        foreach (int size in regionSizeGlobal)
        {
            if (size < minRegionSize)
            {
                return false;
            }
            if (size > maxRegionSize)
            {
                return false;
            }
        }
        return true;
    }

    
}
