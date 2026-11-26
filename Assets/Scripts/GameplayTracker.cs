using UnityEngine;

public class GameplayTracker : MonoBehaviour
{
    public static GameplayTracker Main {get; private set;}

    public int flagsPlaced = 0;

    private SolutionGenerator boardReference;

    void Awake()
    {
        if (Main != null && Main != this)
        {
            Destroy(gameObject);
            return;
        }
        Main = this;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boardReference = SolutionGenerator.Main;
    }

    public bool TryChangeMarks(int delta, bool readOnly=false)
    {
        bool returnStatus = false;
        int newFlags = flagsPlaced + delta;

        if (newFlags >= 0 && newFlags <= boardReference.boardSize)
        {
            returnStatus = true;
            if (!readOnly)
            {
                flagsPlaced = newFlags;
            }
        }

        return returnStatus;
    }

    public void TryCheckForWin()
    {
        if (flagsPlaced == boardReference.boardSize)
        {
            CheckForWin();
        }
    }

    // assumes there are no extra flags placed on board
    // (theoretically, this function will return true if every single tile is flagged)
    public bool CheckForWin()
    {
        for (int i = 0; i < boardReference.boardSize; i++)
        {
            for (int j = 0; j < boardReference.boardSize; j++)
            {
                TileInput currentTile = boardReference.tileReferences[i][j];
                
                if (currentTile.Marked && currentTile.GetTileStatus() != TileInput.TileStatus.Flagged)
                {
                    Debug.Log($"{i}, {j}");
                    return false;
                }

            }
        }
        // stop timer
        GameplayTimer.Main.StopTimer();
        // mark all solutions as valid
        for (int i = 0; i < boardReference.solutionList.Count; i++)
        {
            TileInput solutionTile = boardReference.tileReferences[i][boardReference.solutionList[i]];
            solutionTile.TileCheckedAndAccepted();
        }


        return true;
    }


    
}
