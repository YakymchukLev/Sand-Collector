using UnityEngine;

public class GameStats : MonoBehaviour
{
    private const string CurrentLevelKey = "CurrentLevel";
    private static int currentLevel = 1;

    public static int CurrentLevel
    {
        get => currentLevel;
        set
        {
            currentLevel = value;
            PlayerPrefs.SetInt(CurrentLevelKey, currentLevel);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        LoadCurrentLevelFromPrefs();
    }

    public static void LoadCurrentLevelFromPrefs()
    {
        currentLevel = PlayerPrefs.GetInt(CurrentLevelKey, 1);
    }

    public static void ResetCurrentLevel()
    {
        currentLevel = 1;
        PlayerPrefs.DeleteKey(CurrentLevelKey);
        PlayerPrefs.Save();
    }
}
