using TMPro;
using UnityEngine;

public class GameplayTimer : MonoBehaviour
{
    public static GameplayTimer Main {get; private set;}

    public float timeElapsed;

    private TMP_Text displayText;
    private bool timerRunning;
    private int lastDisplayedTime = -1;
    private int wholeTimeElapsed;


    void Awake()
    {
        if (Main != null && Main != this)
        {
            Destroy(gameObject);
            return;
        }
        Main = this;

        displayText = GetComponent<TMP_Text>();
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
            wholeTimeElapsed = (int)timeElapsed;
            if (wholeTimeElapsed != lastDisplayedTime)
            {
                UpdateTimer();
            }
        }
    }

    private void UpdateTimer()
    {
        lastDisplayedTime = wholeTimeElapsed;
        int seconds = wholeTimeElapsed % 60;
        int minutes = (int)(wholeTimeElapsed / 60);
        displayText.SetText("{0:00}:{1:00}", minutes, seconds);
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
