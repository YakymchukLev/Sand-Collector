using UnityEngine;
using System;
using TMPro;

public class HealthSystem : MonoBehaviour
{
    public static HealthSystem Instance { get; private set; }

    private const int MAX_LIVES = 5;
    private const int TIME_TO_REGEN_SECONDS = 10;
    
    private const string LIVES_KEY = "CurrentLives";
    private const string NEXT_REGEN_TIME_KEY = "NextRegenTime";

    [SerializeField] private int currentLives;
    public int CurrentLives => currentLives;
    public int MaxLives => MAX_LIVES;

    // UI is handled externally

    private DateTime nextRegenTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Робимо об'єкт незнищенним при переході між сценами
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadSystem();
    }

    private void Update()
    {
        if (currentLives < MAX_LIVES)
        {
            if (DateTime.Now >= nextRegenTime)
            {
                currentLives++;
                if (currentLives < MAX_LIVES)
                {
                    nextRegenTime = DateTime.Now.AddSeconds(TIME_TO_REGEN_SECONDS);
                }
                SaveSystem();
            }
        }
        
        // Якщо UI є на цій же сцені, можна оновлювати його тут
    }

    public TimeSpan GetTimeUntilNextLife()
    {
        if (currentLives >= MAX_LIVES) return TimeSpan.Zero;
        TimeSpan timeLeft = nextRegenTime - DateTime.Now;
        return timeLeft.TotalSeconds > 0 ? timeLeft : TimeSpan.Zero;
    }

    private void LoadSystem()
    {
        currentLives = PlayerPrefs.GetInt(LIVES_KEY, MAX_LIVES);
        string timeStr = PlayerPrefs.GetString(NEXT_REGEN_TIME_KEY, string.Empty);
        
        if (currentLives < MAX_LIVES && !string.IsNullOrEmpty(timeStr))
        {
            if (long.TryParse(timeStr, out long temp))
            {
                nextRegenTime = DateTime.FromBinary(temp);
                
                TimeSpan timePassed = DateTime.Now - nextRegenTime;
                if (timePassed.TotalSeconds >= 0)
                {
                    int livesRegenerated = 1 + (int)(timePassed.TotalSeconds / TIME_TO_REGEN_SECONDS);
                    currentLives += livesRegenerated;
                    
                    if (currentLives >= MAX_LIVES)
                    {
                        currentLives = MAX_LIVES;
                    }
                    else
                    {
                        double remainderSeconds = timePassed.TotalSeconds % TIME_TO_REGEN_SECONDS;
                        nextRegenTime = DateTime.Now.AddSeconds(TIME_TO_REGEN_SECONDS - remainderSeconds);
                    }
                }
            }
        }
        else if (currentLives < MAX_LIVES && string.IsNullOrEmpty(timeStr))
        {
            nextRegenTime = DateTime.Now.AddSeconds(TIME_TO_REGEN_SECONDS);
        }
    }

    private void SaveSystem()
    {
        PlayerPrefs.SetInt(LIVES_KEY, currentLives);
        PlayerPrefs.SetString(NEXT_REGEN_TIME_KEY, nextRegenTime.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        SaveSystem();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveSystem();
        }
        else
        {
            LoadSystem();
        }
    }

    public bool CanStartGame()
    {
        return currentLives > 0;
    }

    // Викликайте цей метод безпосередньо перед завантаженням сцени меню
    public void ConsumeLife()
    {
        Debug.Log($"HealthSystem: ConsumeLife викликано. Поточні життя: {currentLives}");
        if (currentLives == MAX_LIVES)
        {
            nextRegenTime = DateTime.Now.AddSeconds(TIME_TO_REGEN_SECONDS);
        }

        currentLives--;
        if (currentLives < 0) currentLives = 0;
        
        SaveSystem();
        Debug.Log($"HealthSystem: Життя збережено. Залишилось: {currentLives}");
    }
}
