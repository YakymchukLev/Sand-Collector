using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveCurrentScene : MonoBehaviour
{
    void Start()
    {
        int activeBuildIndex = SceneManager.GetActiveScene().buildIndex;
        string activeSceneName = SceneManager.GetActiveScene().name;

        if (activeSceneName == "Menu")
        {
            return;
        }

        GameStats.CurrentLevel = activeBuildIndex;
    }

    public static void SaveFinish()
    {
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        int nextBuildIndex = currentBuildIndex + 1;

        if (nextBuildIndex < 0 || nextBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            nextBuildIndex = 1;
        }

        GameStats.CurrentLevel = nextBuildIndex;
        Debug.Log("GameStats.CurrentLevel updated to: " + GameStats.CurrentLevel);
    }
}
