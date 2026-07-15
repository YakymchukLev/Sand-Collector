using UnityEngine;

public class SandCollector : MonoBehaviour
{
    [Header("Налаштування сітки")]
    public int width = 100;
    public int height = 100;

    [HideInInspector]
    public Bucket currentBucket; // Зберігаємо для зворотної сумісності

    [Header("Шлюз")]
    [Range(2, 100)]
    public int gateWidth = 10; // Ширина шлюзу. Задайте 100, щоб пісок висипався по всій ширині!
    
    [Header("Візуалізація")]
    public SpriteRenderer displayRenderer;

    private int[,] grid;
    private Texture2D texture;
    private Color32[] colorBuffer;

    // Лічильники для кожного кольору
    private int currentBlueSandCount;
    private int currentYellowSandCount;
    private int currentRedSandCount;
    private int currentGreenSandCount;
    private int currentOrangeSandCount;

    private bool isGameOver = false;

    public static SandCollector Instance { get; private set; }

    // Загальна кількість кожного кольору (для відер)
    public static int totalBlueSandCount   { get; private set; }
    public static int totalYellowSandCount { get; private set; }
    public static int totalRedSandCount    { get; private set; }
    public static int totalGreenSandCount  { get; private set; }
    public static int totalOrangeSandCount { get; private set; }

    // Типи клітинок
    private const int EMPTY       = 0;
    private const int BLUE_SAND   = 1;
    private const int YELLOW_SAND = 2;
    private const int WALL        = 3;
    private const int RED_SAND    = 4;
    private const int GREEN_SAND  = 5;
    private const int ORANGE_SAND = 6;

    // Кольори для рендерингу
    private static readonly Color32 COLOR_EMPTY  = new Color32(0, 0, 0, 0);
    private static readonly Color32 COLOR_BLUE   = new Color32(30,  100, 255, 255);
    private static readonly Color32 COLOR_YELLOW = new Color32(255, 220,  30, 255);
    private static readonly Color32 COLOR_WALL   = new Color32(100, 100, 100, 255);
    private static readonly Color32 COLOR_RED    = new Color32(220,  40,  40, 255);
    private static readonly Color32 COLOR_GREEN  = new Color32(40,  200,  60, 255);
    private static readonly Color32 COLOR_ORANGE = new Color32(255, 140,  20, 255);

    void Awake()
    {
        Instance = this;

        // Ініціалізуємо сітку тут, а не в Start(),
        // бо LevelManager.Start() може викликати LoadLevelFromTexture() раніше нашого Start()
        grid = new int[width, height];
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        colorBuffer = new Color32[width * height];
    }

    void Start()
    {
        displayRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));

        // Якщо є LevelManager — просимо його завантажити поточний рівень
        // Якщо немає — будуємо стандартний рівень
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadCurrentLevel();
        }
        else
        {
            BuildBordersAndSand();
        }
    }

    void Update()
    {
        UpdateSandPhysics();
        DrawGrid();
    }

    // Створюємо бортики та засипаємо пісок всередину (стандартний рівень без картинки)
    void BuildBordersAndSand()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = EMPTY;

        int wallThickness = 4;
        int bottomY = 10;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < wallThickness) grid[x, y] = WALL;
                if (x >= width - wallThickness) grid[x, y] = WALL;

                if (y >= bottomY && y < bottomY + wallThickness)
                {
                    int halfGate = gateWidth / 2;
                    if (x < width / 2 - halfGate || x > width / 2 + halfGate)
                        grid[x, y] = WALL;
                }
            }
        }

        ResetAllCounters();

        for (int x = wallThickness; x < width - wallThickness; x++)
        {
            for (int y = bottomY + wallThickness; y < height; y++)
            {
                if (y > 40 && y < 55)       { grid[x, y] = BLUE_SAND;   totalBlueSandCount++;   currentBlueSandCount++; }
                else if (y >= 55 && y < 70) { grid[x, y] = YELLOW_SAND; totalYellowSandCount++; currentYellowSandCount++; }
                else if (y >= 70 && y < 80) { grid[x, y] = RED_SAND;    totalRedSandCount++;    currentRedSandCount++; }
                else if (y >= 80 && y < 88) { grid[x, y] = GREEN_SAND;  totalGreenSandCount++;  currentGreenSandCount++; }
                else if (y >= 88 && y < 95) { grid[x, y] = ORANGE_SAND; totalOrangeSandCount++; currentOrangeSandCount++; }
            }
        }

        isGameOver = false;
    }

    void UpdateSandPhysics()
    {
        // Проходимо сітку знизу вгору
        for (int y = 1; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int currentCell = grid[x, y];

                // Працюємо з будь-яким кольором піску
                if (!IsSand(currentCell)) continue;

                // ЛОГІКА РОБОТИ КЛАПАНА/ШЛЮЗУ (y == 10)
                if (y == 10)
                {
                    Bucket bucketAtPos = GetBucketAtGridX(x);
                    if (bucketAtPos != null)
                    {
                        if (bucketAtPos.AddSand(currentCell))
                        {
                            grid[x, y] = EMPTY;
                            DecrementSandCount(currentCell);

                            if (!IsAnySandOfColorLeft(currentCell))
                                bucketAtPos.ForceFull();

                            CheckRemainingSand();
                            continue;
                        }
                        else
                        {
                            continue; // Колір не співпав — шлюз закритий
                        }
                    }
                    else
                    {
                        continue; // Відерця немає — шлюз закритий
                    }
                }

                // СТАНДАРТНИЙ РУХ ПІСКУ
                // 1. Рух прямо вниз
                if (grid[x, y - 1] == EMPTY)
                {
                    grid[x, y - 1] = currentCell;
                    grid[x, y] = EMPTY;
                }
                // 2. Рандомізоване осипання вбік
                else
                {
                    bool checkLeftFirst = Random.value < 0.5f;
                    if (checkLeftFirst)
                    {
                        if (x > 0 && grid[x - 1, y - 1] == EMPTY)
                        { grid[x - 1, y - 1] = currentCell; grid[x, y] = EMPTY; }
                        else if (x < width - 1 && grid[x + 1, y - 1] == EMPTY)
                        { grid[x + 1, y - 1] = currentCell; grid[x, y] = EMPTY; }
                    }
                    else
                    {
                        if (x < width - 1 && grid[x + 1, y - 1] == EMPTY)
                        { grid[x + 1, y - 1] = currentCell; grid[x, y] = EMPTY; }
                        else if (x > 0 && grid[x - 1, y - 1] == EMPTY)
                        { grid[x - 1, y - 1] = currentCell; grid[x, y] = EMPTY; }
                    }
                }
            }
        }
    }

    void DrawGrid()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = x + y * width;
                switch (grid[x, y])
                {
                    case EMPTY:       colorBuffer[pixelIndex] = COLOR_EMPTY;  break;
                    case BLUE_SAND:   colorBuffer[pixelIndex] = COLOR_BLUE;   break;
                    case YELLOW_SAND: colorBuffer[pixelIndex] = COLOR_YELLOW; break;
                    case WALL:        colorBuffer[pixelIndex] = COLOR_WALL;   break;
                    case RED_SAND:    colorBuffer[pixelIndex] = COLOR_RED;    break;
                    case GREEN_SAND:  colorBuffer[pixelIndex] = COLOR_GREEN;  break;
                    case ORANGE_SAND: colorBuffer[pixelIndex] = COLOR_ORANGE; break;
                }
            }
        }

        texture.SetPixels32(colorBuffer);
        texture.Apply();
    }

    Bucket GetBucketAtGridX(int x)
    {
        Bounds bounds = displayRenderer.bounds;
        float spriteWorldWidth = bounds.size.x;
        float worldX = bounds.min.x + ((float)x / width) * spriteWorldWidth;

        Bucket[] buckets = FindObjectsOfType<Bucket>();
        foreach (Bucket bucket in buckets)
        {
            Collider2D bucketCollider = bucket.GetComponent<Collider2D>();
            if (bucketCollider != null)
            {
                if (worldX >= bucketCollider.bounds.min.x && worldX <= bucketCollider.bounds.max.x)
                    return bucket;
            }
            else
            {
                if (Mathf.Abs(bucket.transform.position.x - worldX) < 0.5f)
                    return bucket;
            }
        }
        return null;
    }

    // --- Допоміжні методи ---

    /// <summary>Чи є ця клітинка піском (будь-якого кольору)?</summary>
    private bool IsSand(int cellType)
    {
        return cellType == BLUE_SAND   ||
               cellType == YELLOW_SAND ||
               cellType == RED_SAND    ||
               cellType == GREEN_SAND  ||
               cellType == ORANGE_SAND;
    }

    private void DecrementSandCount(int colorID)
    {
        switch (colorID)
        {
            case BLUE_SAND:   currentBlueSandCount--;   break;
            case YELLOW_SAND: currentYellowSandCount--; break;
            case RED_SAND:    currentRedSandCount--;    break;
            case GREEN_SAND:  currentGreenSandCount--;  break;
            case ORANGE_SAND: currentOrangeSandCount--; break;
        }
    }

    public bool IsAnySandOfColorLeft(int colorID)
    {
        switch (colorID)
        {
            case BLUE_SAND:   return currentBlueSandCount   > 0;
            case YELLOW_SAND: return currentYellowSandCount > 0;
            case RED_SAND:    return currentRedSandCount    > 0;
            case GREEN_SAND:  return currentGreenSandCount  > 0;
            case ORANGE_SAND: return currentOrangeSandCount > 0;
            default: return false;
        }
    }

    private void CheckRemainingSand()
    {
        if (!isGameOver &&
            currentBlueSandCount   <= 0 &&
            currentYellowSandCount <= 0 &&
            currentRedSandCount    <= 0 &&
            currentGreenSandCount  <= 0 &&
            currentOrangeSandCount <= 0)
        {
            isGameOver = true;
            Debug.Log("Весь пісок зібрано! Переходимо до наступного рівня...");
            Invoke(nameof(GoToNextLevel), 2f);
        }
    }

    private void GoToNextLevel()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadNextLevel();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    private void ResetAllCounters()
    {
        totalBlueSandCount   = 0; currentBlueSandCount   = 0;
        totalYellowSandCount = 0; currentYellowSandCount = 0;
        totalRedSandCount    = 0; currentRedSandCount    = 0;
        totalGreenSandCount  = 0; currentGreenSandCount  = 0;
        totalOrangeSandCount = 0; currentOrangeSandCount = 0;
    }

    // ===================================================================
    // ЗАВАНТАЖЕННЯ РІВНЯ З ТЕКСТУРИ
    // ===================================================================

    /// <summary>
    /// Завантажує рівень з текстури. Викликається LevelManager.
    /// Кольори пікселів:
    ///   Чорний  (#000000) → Стіна
    ///   Синій            → Синій пісок
    ///   Жовтий           → Жовтий пісок
    ///   Червоний         → Червоний пісок
    ///   Зелений          → Зелений пісок
    ///   Оранжевий        → Оранжевий пісок
    ///   Білий/прозорий   → Порожньо
    /// ВАЖЛИВО: текстура повинна мати Read/Write Enabled в Inspector!
    /// </summary>
    public void LoadLevelFromTexture(Texture2D levelTexture)
    {
        if (levelTexture == null)
        {
            Debug.LogError("LoadLevelFromTexture: текстура = null!");
            return;
        }

        ResetAllCounters();
        isGameOver = false;

        Color32[] pixels = levelTexture.GetPixels32();
        int texW = levelTexture.width;
        int texH = levelTexture.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Масштабуємо координати текстури під розмір сітки
                int texX = Mathf.Clamp(Mathf.RoundToInt((float)x / width * texW), 0, texW - 1);
                int texY = Mathf.Clamp(Mathf.RoundToInt((float)y / height * texH), 0, texH - 1);

                Color32 pixel = pixels[texX + texY * texW];
                int cellType = ClassifyPixel(pixel);
                grid[x, y] = cellType;

                // Рахуємо кількість кожного типу
                switch (cellType)
                {
                    case BLUE_SAND:   totalBlueSandCount++;   currentBlueSandCount++;   break;
                    case YELLOW_SAND: totalYellowSandCount++; currentYellowSandCount++; break;
                    case RED_SAND:    totalRedSandCount++;    currentRedSandCount++;    break;
                    case GREEN_SAND:  totalGreenSandCount++;  currentGreenSandCount++;  break;
                    case ORANGE_SAND: totalOrangeSandCount++; currentOrangeSandCount++; break;
                }
            }
        }

        Debug.Log($"Рівень '{levelTexture.name}' завантажено: " +
                  $"синій={totalBlueSandCount}, жовтий={totalYellowSandCount}, " +
                  $"червоний={totalRedSandCount}, зелений={totalGreenSandCount}, " +
                  $"оранжевий={totalOrangeSandCount}");
    }

    /// <summary>
    /// Визначає тип клітинки по кольору пікселя.
    /// </summary>
    private int ClassifyPixel(Color32 c)
    {
        // Прозорий або майже прозорий → порожньо
        if (c.a < 50) return EMPTY;

        // Чорний або майже чорний → стіна
        if (c.r < 50 && c.g < 50 && c.b < 50)
            return WALL;

        // Знаходимо домінуючий канал
        float r = c.r;
        float g = c.g;
        float b = c.b;

        // Синій: B >> R і B >> G
        if (b > 150 && b > r + 60 && b > g + 60)
            return BLUE_SAND;

        // Жовтий: R і G великі, B малий
        if (r > 150 && g > 150 && b < 80)
            return YELLOW_SAND;

        // Оранжевий: R великий, G середній, B малий
        if (r > 180 && g > 80 && g < 180 && b < 80)
            return ORANGE_SAND;

        // Червоний: R великий, G і B малі
        if (r > 150 && g < 80 && b < 80)
            return RED_SAND;

        // Зелений: G >> R і G >> B
        if (g > 150 && g > r + 60 && g > b + 60)
            return GREEN_SAND;

        // Все інше — порожньо (білий тощо)
        return EMPTY;
    }
}
