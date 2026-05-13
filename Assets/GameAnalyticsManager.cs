using UnityEngine;
using GameAnalyticsSDK;

public class GameAnalyticsManager : MonoBehaviour
{
    public static GameAnalyticsManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        GameAnalytics.Initialize();
    }

    public void LevelStarted(int level)
    {
        Debug.Log("GA Start Level " + level);

        GameAnalytics.NewProgressionEvent(
            GAProgressionStatus.Start,
            "level_" + level
        );
    }

    public void LevelComplete(int level)
    {
        Debug.Log("GA Complete Level " + level);

        GameAnalytics.NewProgressionEvent(
            GAProgressionStatus.Complete,
            "level_" + level
        );
    }

    public void LevelFailed(int level)
    {
        Debug.Log("GA Fail Level " + level);

        GameAnalytics.NewProgressionEvent(
            GAProgressionStatus.Fail,
            "level_" + level
        );
    }
}