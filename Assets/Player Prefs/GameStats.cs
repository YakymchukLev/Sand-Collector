using UnityEngine;

public class GameStats : MonoBehaviour
{
    private const string CurrentLevelKey = "CurrentLevel";
    private const string CoinsKey = "Coins";
    private const string CompletedLevelsKey = "CompletedLevels";
    private static int currentLevel = 1;
    private static int coins = 0;

    public static int Coins
    {
        get => coins;
        set
        {
            coins = value;
            PlayerPrefs.SetInt(CoinsKey, coins);
            PlayerPrefs.Save();
        }
    }

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
        LoadCoinsFromPrefs();
    }

    public static void LoadCurrentLevelFromPrefs()
    {
        currentLevel = PlayerPrefs.GetInt(CurrentLevelKey, 1);
    }

    public static void LoadCoinsFromPrefs()
    {
        coins = PlayerPrefs.GetInt(CoinsKey, 0);
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        Coins += amount;
    }

    public static void MarkLevelCompleted(int buildIndex)
    {
        if (buildIndex < 0)
            return;

        int completedMask = PlayerPrefs.GetInt(CompletedLevelsKey, 0);
        int bit = 1 << buildIndex;
        if ((completedMask & bit) == 0)
        {
            completedMask |= bit;
            PlayerPrefs.SetInt(CompletedLevelsKey, completedMask);
            PlayerPrefs.Save();
        }
    }

    public static bool IsLevelCompleted(int buildIndex)
    {
        if (buildIndex < 0)
            return false;

        int completedMask = PlayerPrefs.GetInt(CompletedLevelsKey, 0);
        int bit = 1 << buildIndex;
        return (completedMask & bit) != 0;
    }

    public static void ResetCurrentLevel()
    {
        currentLevel = 1;
        PlayerPrefs.DeleteKey(CurrentLevelKey);
        PlayerPrefs.Save();
    }

    public static void ResetCoins()
    {
        coins = 0;
        PlayerPrefs.DeleteKey(CoinsKey);
        PlayerPrefs.Save();
    }
}
