using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class MenuButtonsManager : MonoBehaviour
{
    [SerializeField] private int fallbackSceneBuildIndex = 1;
    [SerializeField] private TMP_Text coinsText;

    [Header("Health UI")]
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject notEnoughLivesPanel;

    [Header("Events")]
    public UnityEvent onNotEnoughLives;

    private void Start()
    {
        GameStats.LoadCoinsFromPrefs();

        if (coinsText != null)
        {
            coinsText.text = GameStats.Coins.ToString();
        }
    }

    private void Update()
    {
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (HealthSystem.Instance == null) return;

        if (livesText != null)
        {
            livesText.text = "Health: " + HealthSystem.Instance.CurrentLives.ToString();
        }

        if (timerText != null)
        {
            if (HealthSystem.Instance.CurrentLives < HealthSystem.Instance.MaxLives)
            {
                System.TimeSpan timeLeft = HealthSystem.Instance.GetTimeUntilNextLife();
                timerText.text = string.Format("{0:D2}:{1:D2}", timeLeft.Minutes, timeLeft.Seconds);
            }
            else
            {
                timerText.text = "MAX";
            }
        }
    }

    public void StartMainGame()
    {
        if (HealthSystem.Instance != null && !HealthSystem.Instance.CanStartGame())
        {
            Debug.Log("Cannot start game: not enough lives.");
            if (notEnoughLivesPanel != null)
            {
                notEnoughLivesPanel.SetActive(true);
            }
            onNotEnoughLives?.Invoke();
            return;
        }

        GameStats.LoadCurrentLevelFromPrefs();

        int targetSceneIndex = GameStats.CurrentLevel;

        if (targetSceneIndex <= 0)
        {
            targetSceneIndex = fallbackSceneBuildIndex;
        }

        if (LevelManager.Instance != null && LevelManager.Instance.TryLoadLevelSceneByBuildIndex(targetSceneIndex))
        {
            return;
        }

        if (targetSceneIndex < 0 || targetSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"MenuButtonsManager: build index {targetSceneIndex} is invalid. Falling back to {fallbackSceneBuildIndex}.");
            targetSceneIndex = fallbackSceneBuildIndex;
        }

        SceneManager.LoadScene(targetSceneIndex);
    }

    public void OpenShop(GameObject shopPanel)
    {
        shopPanel.SetActive(true);
        notEnoughLivesPanel.SetActive(false);
    }
    
    public void CloseShop(GameObject shopPanel)
    {
        shopPanel.SetActive(false);
    }
}
