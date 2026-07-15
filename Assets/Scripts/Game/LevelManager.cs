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

    [Header("Поточний рівень")]
    [Tooltip("З якого рівня починати (0 = перший)")]
    public int startLevelIndex = 0;

    // Індекс поточного активного рівня
    public int CurrentLevelIndex { get; private set; }

    // Загальна кількість рівнів
    public int TotalLevels => levelTextures != null ? levelTextures.Length : 0;

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

    /// <summary>
    /// Завантажити поточний рівень у SandCollector.
    /// </summary>
    public void LoadCurrentLevel()
    {
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

        // Передаємо текстуру в SandCollector
        if (SandCollector.Instance != null)
        {
            SandCollector.Instance.LoadLevelFromTexture(tex);
        }
        else
        {
            Debug.LogError("LevelManager: SandCollector.Instance = null! Переконайтесь що SandCollector є в сцені.");
        }
    }

    /// <summary>
    /// Перейти до наступного рівня. Якщо рівнів більше немає — повертаємось в меню.
    /// </summary>
    public void LoadNextLevel()
    {
        CurrentLevelIndex++;

        if (CurrentLevelIndex >= TotalLevels)
        {
            Debug.Log("LevelManager: всі рівні пройдено! Повертаємось у меню.");
            SceneManager.LoadScene("Menu");
            return;
        }

        Debug.Log($"LevelManager: переходимо на рівень {CurrentLevelIndex + 1}");
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
