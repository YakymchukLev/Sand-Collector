using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Керує рівнями гри. Зберігає масив текстур (по одній на рівень).
/// Кожна текстура — це картинка рівня з папки Sprites/Sprites_LVL/.
/// Кольори пікселів визначають тип клітинки (синій пісок, жовтий пісок, стіна, порожньо).
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Текстури рівнів (Sprites_LVL)")]
    [Tooltip("Перетягніть сюди текстури з папки Sprites/Sprites_LVL/ у потрібному порядку. Індекс 0 = Рівень 1.")]
    public Texture2D[] levelTextures;

    [Header("Сцени рівнів")]
    [Tooltip("Назви сцен для кожного рівня, наприклад: LVL1, LVL2. Якщо заповнено, прогресія буде йти по сценах.")]
    public string[] levelSceneNames;

    [Header("Поточний рівень")]
    [Tooltip("З якого рівня починати (0 = перший)")]
    public int startLevelIndex = 0;

    // Індекс поточного активного рівня
    public int CurrentLevelIndex { get; private set; }

    // Загальна кількість рівнів
    public int TotalLevels
    {
        get
        {
            int textureCount = levelTextures != null ? levelTextures.Length : 0;
            int sceneCount = levelSceneNames != null ? levelSceneNames.Length : 0;
            return Mathf.Max(textureCount, sceneCount);
        }
    }

    void Awake()
    {
        // Singleton — один LevelManager на всю гру
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Зберігаємо між сценами

        CurrentLevelIndex = startLevelIndex;
    }

    void Start()
    {
        // Завантаження рівня тепер ініціюється з SandCollector.Start(),
        // після того як grid та texture вже ініціалізовані.
        // Не викликаємо LoadCurrentLevel() тут, щоб уникнути NullReferenceException.
    }

    private int GetSceneBuildIndex(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return -1;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;

            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryGetLevelIndexFromBuildIndex(int buildIndex, out int levelIndex)
    {
        levelIndex = -1;

        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            return false;
        }

        if (levelSceneNames != null)
        {
            for (int i = 0; i < levelSceneNames.Length; i++)
            {
                string sceneName = levelSceneNames[i];
                if (string.IsNullOrEmpty(sceneName))
                    continue;

                int index = GetSceneBuildIndex(sceneName);
                if (index == buildIndex)
                {
                    levelIndex = i;
                    return true;
                }
            }
        }

        if (buildIndex > 0)
        {
            levelIndex = buildIndex - 1;
            return true;
        }

        return false;
    }

    public bool TryLoadLevelSceneByBuildIndex(int buildIndex)
    {
        if (!TryGetLevelIndexFromBuildIndex(buildIndex, out int levelIndex))
        {
            return false;
        }

        CurrentLevelIndex = levelIndex;
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentBuildIndex == buildIndex)
        {
            GameStats.CurrentLevel = buildIndex;
            return true;
        }

        string sceneName = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(buildIndex));
        if (string.IsNullOrEmpty(sceneName) || sceneName == "Menu")
        {
            return false;
        }

        GameStats.CurrentLevel = buildIndex;
        SceneManager.LoadScene(buildIndex);
        return true;
    }

    public bool TryGetLevelSceneBuildIndex(int levelIndex, out int buildIndex)
    {
        buildIndex = -1;
        if (levelIndex < 0)
        {
            return false;
        }

        if (levelSceneNames != null && levelSceneNames.Length > levelIndex)
        {
            string sceneName = levelSceneNames[levelIndex];
            if (!string.IsNullOrEmpty(sceneName))
            {
                buildIndex = GetSceneBuildIndex(sceneName);
            }
        }

        if (buildIndex < 0)
        {
            buildIndex = levelIndex + 1;
        }

        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            buildIndex = -1;
            return false;
        }

        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        if (string.IsNullOrEmpty(scenePath))
        {
            buildIndex = -1;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Завантажити поточний рівень у SandCollector.
    /// </summary>
    public void LoadCurrentLevel()
    {
        if (TryGetLevelSceneBuildIndex(CurrentLevelIndex, out int targetBuildIndex))
        {
            int activeBuildIndex = SceneManager.GetActiveScene().buildIndex;
            if (activeBuildIndex != targetBuildIndex)
            {
                GameStats.CurrentLevel = targetBuildIndex;
                SceneManager.LoadScene(targetBuildIndex);
                return;
            }

            GameStats.CurrentLevel = targetBuildIndex;
        }

        if (levelTextures == null || levelTextures.Length == 0)
        {
            Debug.LogWarning("LevelManager: масив levelTextures порожній! Додайте текстури в Inspector.");
            return;
        }

        if (CurrentLevelIndex < 0 || CurrentLevelIndex >= levelTextures.Length)
        {
            Debug.LogWarning($"LevelManager: індекс рівня {CurrentLevelIndex} виходить за межі масиву ({levelTextures.Length} рівнів).");
            return;
        }

        Texture2D tex = levelTextures[CurrentLevelIndex];
        if (tex == null)
        {
            Debug.LogWarning($"LevelManager: текстура рівня {CurrentLevelIndex + 1} = null! Перевірте масив в Inspector.");
            return;
        }

        Debug.Log($"LevelManager: завантажуємо рівень {CurrentLevelIndex + 1} з текстури '{tex.name}' ({tex.width}x{tex.height})");

        if (SandCollector.Instance != null)
        {
            SandCollector.Instance.LoadLevelFromTexture(tex);
        }
        else
        {
            Debug.LogError("LevelManager: SandCollector.Instance = null! Переконайтесь що SandCollector є в сцені.");
        }
    }

    public void LoadMainMenu()
    {
        SaveCurrentScene.SaveFinish();
        SceneManager.LoadScene("Menu");
    }

    /// <summary>
    /// Завантажити наступний рівень.
    /// </summary>
    public void LoadNextLevel()
    {
        CurrentLevelIndex++;

        if (CurrentLevelIndex >= TotalLevels)
        {
            LoadMainMenu();
            return;
        }

        LoadCurrentLevel();
    }

    /// <summary>
    /// Завантажити конкретний рівень за індексом (0 = перший рівень).
    /// </summary>
    public void LoadLevel(int index)
    {
        if (index < 0 || index >= TotalLevels)
        {
            Debug.LogWarning($"LevelManager: рівень {index} не існує.");
            return;
        }

        CurrentLevelIndex = index;
        LoadCurrentLevel();
    }

    /// <summary>
    /// Перезавантажити поточний рівень.
    /// </summary>
    public void ReloadCurrentLevel()
    {
        LoadCurrentLevel();
    }
}
