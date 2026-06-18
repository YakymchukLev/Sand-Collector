using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonsManager : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void StartMainGame()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadSceneAsync(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is empty in MenuButtonsManager!");
        }
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
