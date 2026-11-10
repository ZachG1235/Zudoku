using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class SolutionGenerator : MonoBehaviour
{
    public static SolutionGenerator Main {get; private set;}
    [Header("Board Size")]
    [Min(4)]
    public int boardSize = 5;
    
    [Header("Region Sizes")]
    [Tooltip("Smallest Region allowed. 2 is nice. 1 is a free answer.")]
    public int minRegionSize = 3;
    [Tooltip("Recommended value is 'Board Size * 2'")]
    public int maxRegionSize = 8; // try boardSize * 2

    [Header("Seed (0 is random)")]
    public int seed;

    [Space]
    public GameObject columnPrefab;
    public GameObject tilePrefab;

    // this can get privated later i just wanted to see
    public List<int> solutionList = new List<int>();
    public List<Color> squareColors = new List<Color>();

    [HideInInspector] public List<List<TileInput>> tileReferences = new List<List<TileInput>>();

    private List<List<int>> colorRegions = new List<List<int>>(); 
    private bool generating;
    public enum DragStatus
    {
        NotDragging = -1,
        DragCross,
        DragErase
    }
    private DragStatus dragStatus = DragStatus.NotDragging;

    void Awake()
    {
        if (Main != null && Main != this)
        {
            Destroy(gameObject);
            return;
        }

        Main = this;
    }

    void Start()
    { 
        _ = GenerateBoardThreaded();
    }

    private async Awaitable GenerateBoardThreaded()
    {
        if (generating) return;
        generating = true;

        int n = boardSize, min = minRegionSize, max = maxRegionSize, s = seed;
        BoardGenerator gen = null;
        bool ok = false;
        try
        {
            await Awaitable.BackgroundThreadAsync();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            gen = new BoardGenerator(n, s, min, max);
            ok = gen.Generate(regionTries:100, accept: g => g.Solved && g.HardestTier < 3);
            
            // ok = gen.Generate(regionTries: 100, accept: g => g.Solved && g.HardestTier == 4 && g.Steps[4] <= 4);
            sw.Stop();
            Debug.Log($"Generated: {ok} in {sw.ElapsedMilliseconds} ms, solver runs: {gen.SolverRuns}");

            await Awaitable.MainThreadAsync();
        }
        catch (Exception e)
        {
            await Awaitable.MainThreadAsync();
            Debug.LogException(e);
        }
        finally
        {
            generating = false;
        }

        // dont do anything if this object was destoryed while working off thread
        if (this == null) return;

        if (!ok) 
        {
            Debug.LogError("Failed to Generate a Board");
            return; 
        }

        // turn into shapes GenerateGUI expects
        solutionList = new List<int>(gen.Marks);
        colorRegions = new List<List<int>>();
        for (int r = 0; r < n; r++)
        {
            var row = new List<int>();
            for (int c = 0; c < n; c++)
            {
                row.Add(gen.RegionOf[r * n + c]);
            }
            colorRegions.Add(row);
        }

        GenerateGUI();
    }

    private void GenerateGUI()
    {
        Transform rowManager = transform.GetChild(0);
        for (int i = 0; i < boardSize; i++)
        {
            var tileRowList = new List<TileInput>();
            Transform currentRow = Instantiate(columnPrefab, rowManager).transform;
            for (int j = 0; j < boardSize; j++)
            {
                GameObject tile = Instantiate(tilePrefab, currentRow);
                Image tileImage = tile.GetComponent<Image>();
                tileImage.color = squareColors[colorRegions[i][j]];

                TileInput input = tile.GetComponent<TileInput>();
                input.Init(i, j, solutionList[i] == j);
                input.Clicked += OnTileClicked;
                input.DoubleClicked += OnTileDoubleClicked;
                input.DragStarted += OnTileDragEntered;
                input.DragEntered += OnTileDragEntered;
                input.DragEnded += OnTileDragEnded;
                tileRowList.Add(input);
            }
            tileReferences.Add(tileRowList);
        }
    }

    private void OnTileClicked(TileInput tile)
    {
        // Debug.Log($"Clicked {tile.Row}, {tile.Col}");
        if (tile.isLocked) return;
        tile.ToggleCross();
    }

    private void OnTileDoubleClicked(TileInput tile)
    {
        // Debug.Log($"Double-clicked {tile.Row}, {tile.Col}");
        if (tile.isLocked) return;
        tile.SetDisplayFlag();
    }

    private void OnTileDragEntered(TileInput tile)
    {
        if (tile.isLocked) return;
        // Debug.Log($"Drag over {tile.Row}, {tile.Col}");
        if (tile.GetTileStatus() == TileInput.TileStatus.Flagged)
        {
            return;
        }
        
        if (dragStatus == DragStatus.NotDragging)
        {
            // set drag status to what we just changed the icon to 
            bool isCrossed = tile.IsCurrentlyCrossed();
            
            if (isCrossed)
            {
                dragStatus = DragStatus.DragErase;
            }
            else
            {
                dragStatus = DragStatus.DragCross;
            }
            tile.ToggleCross();
        }
        else if (dragStatus == DragStatus.DragCross)
        {
            tile.SetCross();
        }
        else if (dragStatus == DragStatus.DragErase)
        {
            tile.SetEmpty();
        }
    }

    private void OnTileDragEnded(TileInput tile)
    {
        if (tile.isLocked) return;
        dragStatus = DragStatus.NotDragging;
    }
}
