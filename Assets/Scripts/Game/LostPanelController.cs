using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

public class LostPanelController : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private GameObject notEnoughLivesPanel;

    [Header("Events")]
    public UnityEvent onNotEnoughLives;

    public void RestartScene()
    {
        if (HealthSystem.Instance != null && !HealthSystem.Instance.CanStartGame())
        {
            Debug.Log("Cannot restart game: not enough lives.");
            if (notEnoughLivesPanel != null)
            {
                notEnoughLivesPanel.SetActive(true);
            }
            onNotEnoughLives?.Invoke();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    public void ContinueToNextLevel()
    {
        Time.timeScale = 1f;

        int activeBuildIndex = SceneManager.GetActiveScene().buildIndex;
        GameStats.MarkLevelCompleted(activeBuildIndex);

        int nextBuildIndex = activeBuildIndex + 1;
        if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            GameStats.CurrentLevel = nextBuildIndex;
            SceneManager.LoadScene(nextBuildIndex);
            return;
        }

        GameStats.CurrentLevel = activeBuildIndex;
        SceneManager.LoadScene("Menu");
    }

    public void BindContinueButton(GameObject panelRoot)
    {
        if (panelRoot == null)
            return;

        Button[] buttons = panelRoot.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null)
                continue;

            string buttonName = button.name.ToLowerInvariant();
            if (buttonName.Contains("continue") || buttonName.Contains("next") || buttonName.Contains("nextlevel"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ContinueToNextLevel);
                return;
            }
        }
    }

    public void Plus40Coins()
    {
        Time.timeScale = 1f;
        GameStats.AddCoins(40);
        RefreshCoinDisplay();
        SceneManager.LoadScene("Menu");
    }

    public void Plus80Coins()
    {
        Time.timeScale = 1f;
        GameStats.AddCoins(80);
        RefreshCoinDisplay();
        SceneManager.LoadScene("Menu");
    }

    private void RefreshCoinDisplay()
    {
        var tmpTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in tmpTexts)
        {
            if (text != null && text.name.ToLowerInvariant().Contains("coin"))
            {
                text.text = $"Coins: {GameStats.Coins}";
                return;
            }
        }

        var legacyTexts = GetComponentsInChildren<Text>(true);
        foreach (var text in legacyTexts)
        {
            if (text != null && text.name.ToLowerInvariant().Contains("coin"))
            {
                text.text = $"Coins: {GameStats.Coins}";
                return;
            }
        }
    }
}
