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
    private int currentWhiteSandCount;
    private int currentBlackSandCount;

    private bool isGameOver = false;

    public static SandCollector Instance { get; private set; }

    // Загальна кількість кожного кольору (для відер)
    public static int totalBlueSandCount   { get; private set; }
    public static int totalYellowSandCount { get; private set; }
    public static int totalRedSandCount    { get; private set; }
    public static int totalGreenSandCount  { get; private set; }
    public static int totalOrangeSandCount { get; private set; }
    public static int totalWhiteSandCount  { get; private set; }
    public static int totalBlackSandCount  { get; private set; }

    // Типи клітинок
    private const int EMPTY       = 0;
    private const int BLUE_SAND   = 1;
    private const int YELLOW_SAND = 2;
    private const int WALL        = 3;
    private const int RED_SAND    = 4;
    private const int GREEN_SAND  = 5;
    private const int ORANGE_SAND = 6;
    private const int WHITE_SAND  = 7;
    private const int BLACK_SAND  = 8;

    // Кольори для рендерингу
    private static readonly Color32 COLOR_EMPTY  = new Color32(0, 0, 0, 0);
    private static readonly Color32 COLOR_BLUE   = new Color32(30,  100, 255, 255);
    private static readonly Color32 COLOR_YELLOW = new Color32(255, 220,  30, 255);
    private static readonly Color32 COLOR_WALL   = new Color32(100, 100, 100, 255);
    private static readonly Color32 COLOR_RED    = new Color32(220,  40,  40, 255);
    private static readonly Color32 COLOR_GREEN  = new Color32(40,  200,  60, 255);
    private static readonly Color32 COLOR_ORANGE = new Color32(255, 140,  20, 255);
    private static readonly Color32 COLOR_WHITE  = new Color32(255, 255, 255, 255);
    private static readonly Color32 COLOR_BLACK  = new Color32(20,  20,  20, 255);

    void Awake()
    {
        Instance = this;

        grid = new int[width, height];
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        colorBuffer = new Color32[width * height];
    }

    void Start()
    {
        displayRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));

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

    private void ClearExistingBucketsAndSpawners()
    {
        Bucket[] existingBuckets = FindObjectsOfType<Bucket>();
        foreach (Bucket bucket in existingBuckets)
        {
            if (bucket != null)
            {
                bucket.RemoveFromScene(true);
            }
        }

        ConveyorManager[] conveyorManagers = FindObjectsOfType<ConveyorManager>();
        foreach (ConveyorManager manager in conveyorManagers)
        {
            if (manager != null)
            {
                manager.StopSpawning();
            }
        }
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
        // Обробляємо y=0 окремо — пісок на самому дні збирається відерцями
        for (int x = 0; x < width; x++)
        {
            int currentCell = grid[x, 0];
            if (!IsSand(currentCell)) continue;

            Bucket bucketAtPos = GetBucketAtGridX(x);
            if (bucketAtPos != null)
            {
                if (bucketAtPos.AddSand(currentCell))
                {
                    grid[x, 0] = EMPTY;
                    DecrementSandCount(currentCell);

                    if (!IsAnySandOfColorLeft(currentCell))
                        bucketAtPos.ForceFull();

                    CheckRemainingSand();
                }
            }
        }

        for (int y = 1; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int currentCell = grid[x, y];

                if (!IsSand(currentCell)) continue;

                // Спроба впасти вниз
                if (grid[x, y - 1] == EMPTY)
                {
                    grid[x, y - 1] = currentCell;
                    grid[x, y] = EMPTY;
                }
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

                // Якщо піщинка тепер на найнижчому рядку (y=1 впала на y=0), 
                // або якщо вона стоїть і не може впасти — спробуємо зібрати відерцем
                if (y == 1 && grid[x, y] == currentCell)
                {
                    // Піщинка не змогла впасти — вона застрягла, спробуємо зібрати
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
                        }
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
                    case WHITE_SAND:  colorBuffer[pixelIndex] = COLOR_WHITE;  break;
                    case BLACK_SAND:  colorBuffer[pixelIndex] = COLOR_BLACK;  break;
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

    private bool IsSand(int cellType)
    {
        return cellType == BLUE_SAND   ||
               cellType == YELLOW_SAND ||
               cellType == RED_SAND    ||
               cellType == GREEN_SAND  ||
               cellType == ORANGE_SAND ||
               cellType == WHITE_SAND  ||
               cellType == BLACK_SAND;
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
            case WHITE_SAND:  currentWhiteSandCount--;  break;
            case BLACK_SAND:  currentBlackSandCount--;  break;
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
            case WHITE_SAND:  return currentWhiteSandCount  > 0;
            case BLACK_SAND:  return currentBlackSandCount  > 0;
            default: return false;
        }
    }

    /// <summary>
    /// Повертає список ідентифікаторів кольорів піску, які реально присутні на малюнку поточного рівня.
    /// </summary>
    public System.Collections.Generic.List<int> GetActiveSandColorIDs()
    {
        var activeColors = new System.Collections.Generic.List<int>();
        if (totalBlueSandCount > 0)   activeColors.Add(BLUE_SAND);   // 1
        if (totalYellowSandCount > 0) activeColors.Add(YELLOW_SAND); // 2
        if (totalRedSandCount > 0)    activeColors.Add(RED_SAND);    // 4
        if (totalGreenSandCount > 0)  activeColors.Add(GREEN_SAND);  // 5
        if (totalOrangeSandCount > 0) activeColors.Add(ORANGE_SAND); // 6
        if (totalWhiteSandCount > 0)  activeColors.Add(WHITE_SAND);  // 7
        if (totalBlackSandCount > 0)  activeColors.Add(BLACK_SAND);  // 8
        return activeColors;
    }

    private void CheckRemainingSand()
    {
        if (!isGameOver &&
            currentBlueSandCount   <= 0 &&
            currentYellowSandCount <= 0 &&
            currentRedSandCount    <= 0 &&
            currentGreenSandCount  <= 0 &&
            currentOrangeSandCount <= 0 &&
            currentWhiteSandCount  <= 0 &&
            currentBlackSandCount  <= 0)
        {
            isGameOver = true;
            Debug.Log("Весь пісок зібрано! Повертаємося в меню...");
            Invoke(nameof(GoToMenu), 2f);
        }
    }

    private void GoToMenu()
    {
        ClearExistingBucketsAndSpawners();

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadMainMenu();
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
        totalWhiteSandCount  = 0; currentWhiteSandCount  = 0;
        totalBlackSandCount  = 0; currentBlackSandCount  = 0;
    }

    public void LoadLevelFromTexture(Texture2D levelTexture)
    {
        if (levelTexture == null)
        {
            Debug.LogError("LoadLevelFromTexture: текстура = null!");
            return;
        }

        ClearExistingBucketsAndSpawners();
        ResetAllCounters();
        isGameOver = false;

        Color32[] pixels = levelTexture.GetPixels32();
        int texW = levelTexture.width;
        int texH = levelTexture.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int texX = Mathf.Clamp(Mathf.RoundToInt((float)x / width * texW), 0, texW - 1);
                int texY = Mathf.Clamp(Mathf.RoundToInt((float)y / height * texH), 0, texH - 1);

                Color32 pixel = pixels[texX + texY * texW];
                int cellType = ClassifyPixel(pixel);
                grid[x, y] = cellType;

                switch (cellType)
                {
                    case BLUE_SAND:   totalBlueSandCount++;   currentBlueSandCount++;   break;
                    case YELLOW_SAND: totalYellowSandCount++; currentYellowSandCount++; break;
                    case RED_SAND:    totalRedSandCount++;    currentRedSandCount++;    break;
                    case GREEN_SAND:  totalGreenSandCount++;  currentGreenSandCount++;  break;
                    case ORANGE_SAND: totalOrangeSandCount++; currentOrangeSandCount++; break;
                    case WHITE_SAND:  totalWhiteSandCount++;  currentWhiteSandCount++;  break;
                    case BLACK_SAND:  totalBlackSandCount++;  currentBlackSandCount++;  break;
                }
            }
        }

        Debug.Log($"Рівень '{levelTexture.name}' завантажено: " +
                  $"синій={totalBlueSandCount}, жовтий={totalYellowSandCount}, " +
                  $"червоний={totalRedSandCount}, зелений={totalGreenSandCount}, " +
                  $"оранжевий={totalOrangeSandCount}, білий={totalWhiteSandCount}, чорний={totalBlackSandCount}");
    }

    private int ClassifyPixel(Color32 c)
    {
        // 1. Прозорий або майже прозорий → порожньо
        if (c.a < 50) return EMPTY;

        // Конвертуємо колір у формат HSV (Hue, Saturation, Value)
        Color.RGBToHSV(c, out float h, out float s, out float v);

        // 2. Чорний або дуже темний колір
        if (v < 0.15f) return BLACK_SAND;

        // 3. Відтінки сірого (низька насиченість)
        if (s < 0.2f)
        {
            if (v > 0.72f) return WHITE_SAND; // Світло-сірий або білий
            return WALL; // Середньо-сірий - це стіна
        }

        // 4. Класифікація кольорових пікселів за відтінком (Hue)
        if (h < 0.05f || h > 0.90f) return RED_SAND;
        if (h >= 0.05f && h < 0.11f) return ORANGE_SAND;
        if (h >= 0.11f && h < 0.22f) return YELLOW_SAND;
        if (h >= 0.22f && h < 0.45f) return GREEN_SAND;
        if (h >= 0.45f && h <= 0.90f) return BLUE_SAND;

        return WALL; // Резервний варіант
    }
}