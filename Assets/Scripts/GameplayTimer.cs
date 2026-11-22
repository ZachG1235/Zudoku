using UnityEngine;

public class GameplayTimer : MonoBehaviour
{
    public static GameplayTimer Main {get; private set;}

    public float timeElapsed;

    bool timerRunning;


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
        timeElapsed = 0;
        timerRunning = false;
    }

    void Update()
    {
        if (timerRunning)
        {
            timeElapsed += Time.deltaTime;
        } 
    }



    public void StopTimer()
    {
        timerRunning = false;
    }

    public void ResumeTimer()
    {
        timerRunning = true;
    }

    public void ToggleTimer()
    {
        timerRunning = !timerRunning;
    }

    public void SetTimerState(bool nextState)
    {
        timerRunning = nextState;
    }

    public void ResetTimer()
    {
        timeElapsed = 0f;
    }
}
