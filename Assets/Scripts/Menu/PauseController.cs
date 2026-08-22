using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject notEnoughLivesPanel;

    private void Start()
    {
        pauseMenuUI.SetActive(false);
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        pauseMenuUI.SetActive(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        HealthSystem.Instance?.ConsumeLife();
        SceneManager.LoadScene("Menu");
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
