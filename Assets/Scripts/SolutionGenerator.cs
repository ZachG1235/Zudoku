using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SolutionGenerator : MonoBehaviour
{
    public int boardSize = 5;
    public GameObject columnPrefab;
    public GameObject tilePrefab;

    // private List<List<bool>> solutionGrid = new List<List<bool>>();
    public List<int> solutionList = new List<int>();

    public List<Color> squareColors = new List<Color>();


    private List<bool> usedList = new List<bool>();
    private List<List<int>> colorRegions = new List<List<int>>();


    private static readonly int[] dRow = {-1, 1, 0, 0};
    private static readonly int[] dCol = {0, 0, -1, 1};

    // lesson 3 helpers
    List<(int row, int c)>[] regionCells = null;
    bool[] rowUsed;
    bool[] colUsed;
    bool[] regionDone;
    int[] solutionColInRow;


    void Awake()
    {
        // TODO: make this a singleton
    }

    void Start()
    { 
        CreateSolutionList();
        colorRegions = LoadRegionIds();
        GenerateGUI();
    }

    private void CreateSolutionList()
    {
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
            int region = Random.Range(0, boardSize);

            List<(int row, int c)> frontier = GetColorFrontier(region, boardSize, regionId);
            // if empty, continue
            if (frontier.Count == 0)
            {
                continue;
            }

            (int row, int c) chosen = frontier[Random.Range(0, frontier.Count)];

            regionId[chosen.row][chosen.c] = region; 
            regionSize[region] += 1;
            unclaimed -= 1; 
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
                    tile.color = Color.black;
                }
                else
                {
                    tile.color = squareColors[colorRegions[i][j]];
                }
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
        if (r < boardSize && solutionColInRow[r + 1] != -1 && Mathf.Abs(c - solutionColInRow[r + 1]) <= 1) return false;

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
            if regionDone
        }
    }

    
}
