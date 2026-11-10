using UnityEngine;

public class GameplayUserInterface : MonoBehaviour
{
    private SolutionGenerator solutionGenerator;

    void Start()
    {
        solutionGenerator = SolutionGenerator.Main;
    }

    public void ShowAnswerChecks()
    {
        for (int i = 0; i < solutionGenerator.boardSize; i++)
        {
            for (int j = 0; j < solutionGenerator.boardSize; j++)
            {
                TileInput currentTile = solutionGenerator.tileReferences[i][j];
                if (currentTile.GetTileStatus() == TileInput.TileStatus.Flagged)
                {
                    if (currentTile.Marked)
                    {
                        currentTile.SetImageColor(Color.green);
                        currentTile.isLocked = true;
                    }
                    else
                    {
                        currentTile.SetImageColor(Color.red);
                    }
                }
            }
        }
    }
}
