using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonsManager : MonoBehaviour
{
    [SerializeField] private int fallbackSceneBuildIndex = 1;

    public void StartMainGame()
    {
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
    }
    
    public void CloseShop(GameObject shopPanel)
    {
        shopPanel.SetActive(false);
    }
}
