using UnityEngine;
using System.Collections;
using UnityEngine.Events;

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

    [Header("Збір піску")]
    [Tooltip("Висота над відром, з якої воно може забирати пісок, якщо інший колір заважає йому впасти на саме дно")]
    public int collectionHeight = 5;

    [Tooltip("Кількість пікселів, яка вважається 'шумом' і зливається з найпопулярнішим кольором. Допомагає прибирати артефакти стиснення картинок.")]
    public int noiseThreshold = 150;

    [Tooltip("Мінімальний розмір 'острівця' стіни. Усі шматки стіни менші за цей розмір будуть видалені. Прибирає сміття у повітрі.")]
    public int minWallIslandSize = 50;

    [Header("Область малюнка (Відступи від країв)")]
    [Tooltip("Зменшує область, в якій генерується пісок з картинки")]
    public int paddingLeft = 0;
    public int paddingRight = 0;
    public int paddingTop = 0;
    public int paddingBottom = 0;
    
    [Header("Візуалізація")]
    public SpriteRenderer displayRenderer;

    private int[,] grid;
    private Color32[,] cellColors;
    private Texture2D texture;
    private Color32[] colorBuffer;

    [HideInInspector]
    public System.Collections.Generic.List<Bucket> activeBucketsList = new System.Collections.Generic.List<Bucket>();
    private ConveyorManager cachedConveyor;

    // Лічильники для кожного кольору
    private int currentBlueSandCount;
    private int currentYellowSandCount;
    private int currentRedSandCount;
    private int currentGreenSandCount;
    private int currentOrangeSandCount;
    private int currentWhiteSandCount;
    private int currentBlackSandCount;

    public GameObject losePanel; // Assign LosePanel UI in Inspector
    public GameObject winPanel; // Assign WinPanel UI in Inspector

    [Header("Події завершення рівня")]
    public UnityEvent onLevelLost;
    public UnityEvent onLevelWon;

    private bool isGameOver = false;
    private bool isLevelCompleted = false;

    // Таймер програшу: коли всі слоти зайняті і жодне відро не заповнене
    [Header("Програш")]
    [Tooltip("Скільки секунд чекати перед програшем, коли всі слоти зайняті")]
    public float loseTimerDuration = 5f;
    private float loseTimer = 0f;
    private bool loseTimerActive = false;

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
        isLevelCompleted = false;
        cachedConveyor = FindFirstObjectByType<ConveyorManager>();

        grid = new int[width, height];
        cellColors = new Color32[width, height];
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
        // Check lose condition after physics update
        if (!isGameOver)
        {
            CheckLoseCondition();
        }
    }

    private void ClearExistingBucketsAndSpawners()
    {
        Bucket[] existingBuckets = FindObjectsByType<Bucket>(FindObjectsSortMode.None);
        foreach (Bucket bucket in existingBuckets)
        {
            if (bucket != null)
            {
                bucket.RemoveFromScene(true);
            }
        }

        ConveyorManager[] conveyorManagers = FindObjectsByType<ConveyorManager>(FindObjectsSortMode.None);
        foreach (ConveyorManager manager in conveyorManagers)
        {
            if (manager != null)
            {
                manager.StopSpawning();
            }
        }

        BucketSpawnerButton[] spawnerButtons = FindObjectsByType<BucketSpawnerButton>(FindObjectsSortMode.None);
        foreach (BucketSpawnerButton btn in spawnerButtons)
        {
            if (btn != null)
            {
                btn.ResetState();
            }
        }
    }

    private Color32 GetDefaultColorForType(int cellType)
    {
        switch (cellType)
        {
            case EMPTY:       return COLOR_EMPTY;
            case BLUE_SAND:   return COLOR_BLUE;
            case YELLOW_SAND: return COLOR_YELLOW;
            case WALL:        return COLOR_WALL;
            case RED_SAND:    return COLOR_RED;
            case GREEN_SAND:  return COLOR_GREEN;
            case ORANGE_SAND: return COLOR_ORANGE;
            case WHITE_SAND:  return COLOR_WHITE;
            case BLACK_SAND:  return COLOR_BLACK;
            default:          return COLOR_EMPTY;
        }
    }

    private void MoveCell(int fromX, int fromY, int toX, int toY)
    {
        grid[toX, toY] = grid[fromX, fromY];
        cellColors[toX, toY] = cellColors[fromX, fromY];
        grid[fromX, fromY] = EMPTY;
        cellColors[fromX, fromY] = COLOR_EMPTY;
    }

    private void ClearCell(int x, int y)
    {
        grid[x, y] = EMPTY;
        cellColors[x, y] = COLOR_EMPTY;
    }

    private void RemoveSmallWallIslands(int minSize)
    {
        bool[,] visited = new bool[width, height];
        System.Collections.Generic.List<Vector2Int> currentIsland = new System.Collections.Generic.List<Vector2Int>();
        System.Collections.Generic.Queue<Vector2Int> queue = new System.Collections.Generic.Queue<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == WALL && !visited[x, y])
                {
                    currentIsland.Clear();
                    queue.Clear();
                    
                    queue.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        Vector2Int p = queue.Dequeue();
                        currentIsland.Add(p);

                        Vector2Int[] neighbors = {
                            new Vector2Int(p.x + 1, p.y),
                            new Vector2Int(p.x - 1, p.y),
                            new Vector2Int(p.x, p.y + 1),
                            new Vector2Int(p.x, p.y - 1)
                        };

                        foreach (var n in neighbors)
                        {
                            if (n.x >= 0 && n.x < width && n.y >= 0 && n.y < height)
                            {
                                if (grid[n.x, n.y] == WALL && !visited[n.x, n.y])
                                {
                                    visited[n.x, n.y] = true;
                                    queue.Enqueue(n);
                                }
                            }
                        }
                    }

                    // Якщо острівець стіни замалий - перетворюємо його на повітря (EMPTY)
                    if (currentIsland.Count < minSize)
                    {
                        foreach (var p in currentIsland)
                        {
                            grid[p.x, p.y] = EMPTY;
                            cellColors[p.x, p.y] = COLOR_EMPTY;
                        }
                    }
                }
            }
        }
    }

    // Створюємо бортики та засипаємо пісок всередину (стандартний рівень без картинки)
    void BuildBordersAndSand()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = EMPTY;
                cellColors[x, y] = COLOR_EMPTY;
            }

        int wallThickness = 4;
        int bottomY = 10;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < wallThickness) { grid[x, y] = WALL; cellColors[x, y] = COLOR_WALL; }
                if (x >= width - wallThickness) { grid[x, y] = WALL; cellColors[x, y] = COLOR_WALL; }

                if (y >= bottomY && y < bottomY + wallThickness)
                {
                    int halfGate = gateWidth / 2;
                    if (x < width / 2 - halfGate || x > width / 2 + halfGate)
                    {
                        grid[x, y] = WALL;
                        cellColors[x, y] = COLOR_WALL;
                    }
                }
            }
        }

        ResetAllCounters();

        for (int x = wallThickness; x < width - wallThickness; x++)
        {
            for (int y = bottomY + wallThickness; y < height; y++)
            {
                if (y > 40 && y < 55)       { grid[x, y] = BLUE_SAND;   cellColors[x, y] = COLOR_BLUE;   totalBlueSandCount++;   currentBlueSandCount++; }
                else if (y >= 55 && y < 70) { grid[x, y] = YELLOW_SAND; cellColors[x, y] = COLOR_YELLOW; totalYellowSandCount++; currentYellowSandCount++; }
                else if (y >= 70 && y < 80) { grid[x, y] = RED_SAND;    cellColors[x, y] = COLOR_RED;    totalRedSandCount++;    currentRedSandCount++; }
                else if (y >= 80 && y < 88) { grid[x, y] = GREEN_SAND;  cellColors[x, y] = COLOR_GREEN;  totalGreenSandCount++;  currentGreenSandCount++; }
                else if (y >= 88 && y < 95) { grid[x, y] = RED_SAND;    cellColors[x, y] = COLOR_RED;    totalRedSandCount++;    currentRedSandCount++; }
            }
        }

        isGameOver = false;
        ResetLoseTimer();
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
                if (bucketAtPos.AddSand(currentCell, cellColors[x, 0]))
                {
                    if (SandVisuals.Instance != null)
                    {
                        Bounds bounds = displayRenderer.bounds;
                        float spriteWorldWidth = bounds.size.x;
                        float worldX = bounds.min.x + ((float)x / width) * spriteWorldWidth;
                        float worldY = bounds.min.y;
                        Vector3 startPos = new Vector3(worldX, worldY, displayRenderer.transform.position.z - 0.1f);
                        float visualScale = (spriteWorldWidth / width) * 100f;
                        SandVisuals.Instance.SpawnFlyingSand(startPos, bucketAtPos.transform, cellColors[x, 0], visualScale);
                    }

                    ClearCell(x, 0);
                    DecrementSandCount(currentCell);

                    if (!IsAnySandOfColorLeft(bucketAtPos.targetColorID))
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
                    MoveCell(x, y, x, y - 1);
                }
                else
                {
                    bool checkLeftFirst = Random.value < 0.5f;
                    if (checkLeftFirst)
                    {
                        if (x > 0 && grid[x - 1, y - 1] == EMPTY)
                        { MoveCell(x, y, x - 1, y - 1); }
                        else if (x < width - 1 && grid[x + 1, y - 1] == EMPTY)
                        { MoveCell(x, y, x + 1, y - 1); }
                    }
                    else
                    {
                        if (x < width - 1 && grid[x + 1, y - 1] == EMPTY)
                        { MoveCell(x, y, x + 1, y - 1); }
                        else if (x > 0 && grid[x - 1, y - 1] == EMPTY)
                        { MoveCell(x, y, x - 1, y - 1); }
                    }
                }

                // Якщо піщинка тепер знаходиться в межах висоти збору (collectionHeight), 
                // і якщо вона стоїть і не може впасти — спробуємо зібрати відерцем
                if (y <= collectionHeight && grid[x, y] == currentCell)
                {
                    // Піщинка не змогла впасти — вона застрягла, спробуємо зібрати
                    Bucket bucketAtPos = GetBucketAtGridX(x);
                    if (bucketAtPos != null)
                    {
                        if (bucketAtPos.AddSand(currentCell, cellColors[x, y]))
                        {
                            if (SandVisuals.Instance != null)
                            {
                                Bounds bounds = displayRenderer.bounds;
                                float spriteWorldWidth = bounds.size.x;
                                float spriteWorldHeight = bounds.size.y;
                                float worldX = bounds.min.x + ((float)x / width) * spriteWorldWidth;
                                float worldY = bounds.min.y + ((float)y / height) * spriteWorldHeight;
                                Vector3 startPos = new Vector3(worldX, worldY, displayRenderer.transform.position.z - 0.1f);
                                float visualScale = (spriteWorldWidth / width) * 100f;
                                SandVisuals.Instance.SpawnFlyingSand(startPos, bucketAtPos.transform, cellColors[x, y], visualScale);
                            }

                            ClearCell(x, y);
                            DecrementSandCount(currentCell);

                            if (!IsAnySandOfColorLeft(bucketAtPos.targetColorID))
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
                colorBuffer[pixelIndex] = cellColors[x, y];
            }
        }

        texture.SetPixels32(colorBuffer);
        texture.Apply(false); // ВАЖЛИВО ДЛЯ ОПТИМІЗАЦІЇ: false вимикає генерацію Mipmaps, що суттєво розвантажує GPU
    }

    Bucket GetBucketAtGridX(int x)
    {
        Bounds bounds = displayRenderer.bounds;
        float spriteWorldWidth = bounds.size.x;
        float worldX = bounds.min.x + ((float)x / width) * spriteWorldWidth;

        foreach (Bucket bucket in activeBucketsList)
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

    private void CheckLoseCondition()
    {
        if (cachedConveyor == null)
            cachedConveyor = FindFirstObjectByType<ConveyorManager>();
            
        if (cachedConveyor == null) return;

        bool hasAnyActiveBucket = activeBucketsList.Count > 0;
        bool hasFreeSlot = activeBucketsList.Count < cachedConveyor.maxActiveBuckets;
        bool hasFullBucket = false;
        bool isAnyBucketFilling = false;

        foreach (var bucket in activeBucketsList)
        {
            if (bucket == null)
                continue;

            if (bucket.IsFull)
            {
                hasFullBucket = true;
                break;
            }
            if (Time.time - bucket.lastSandTime < 1.0f)
            {
                isAnyBucketFilling = true;
            }
        }

        if (!hasAnyActiveBucket)
        {
            ResetLoseTimer();
            return;
        }

        if (hasFreeSlot || hasFullBucket || isAnyBucketFilling)
        {
            ResetLoseTimer();
            return;
        }

        if (!loseTimerActive)
        {
            loseTimerActive = true;
            loseTimer = 0f;
            Debug.Log($"Lose timer started: {loseTimerDuration} seconds to fill a bucket!");
        }

        loseTimer += Time.deltaTime;

        if (loseTimer >= loseTimerDuration)
        {
            TriggerGameOver();
        }
    }

    private void ResetLoseTimer()
    {
        if (loseTimerActive)
        {
            Debug.Log("Lose timer reset.");
        }
        loseTimerActive = false;
        loseTimer = 0f;
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over: All bucket slots occupied and none filled within time limit.");

        if (HealthSystem.Instance != null)
        {
            Debug.Log($"TriggerGameOver: HealthSystem знайшли, віднімаємо життя. Життів до: {HealthSystem.Instance.CurrentLives}");
            HealthSystem.Instance.ConsumeLife();
            Debug.Log($"TriggerGameOver: Життів після: {HealthSystem.Instance.CurrentLives}");
        }
        else
        {
            Debug.LogError("TriggerGameOver: HealthSystem.Instance дорівнює NULL! Життя не віднімається (можливо, ви запустили рівень напряму, оминувши головне меню).");
        }
        
        onLevelLost?.Invoke();

        // Вмикаємо панель програшу
        if (losePanel != null)
        {
            losePanel.SetActive(true);
        }

        // Вимикаємо кнопки спавна відер
        BucketSpawnerButton[] spawnerButtons = FindObjectsByType<BucketSpawnerButton>(FindObjectsSortMode.None);
        foreach (BucketSpawnerButton btn in spawnerButtons)
        {
            if (btn != null)
            {
                btn.DisableForGameOver();
            }
        }

        // Вимикаємо відра на конвеєрі
        Bucket[] activeBuckets = FindObjectsByType<Bucket>(FindObjectsSortMode.None);
        foreach (Bucket bucket in activeBuckets)
        {
            if (bucket != null)
            {
                bucket.DisableForGameOver();
            }
        }

        // Зупиняємо спавн конвеєра
        ConveyorManager[] conveyors = FindObjectsByType<ConveyorManager>(FindObjectsSortMode.None);
        foreach (ConveyorManager cm in conveyors)
        {
            if (cm != null)
                cm.StopSpawning();
        }

        // Зупиняємо гру (UI продовжує працювати)
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Перевіряє чи залишився пісок на сітці. Якщо ні — рівень пройдено.
    /// </summary>
    private void CheckRemainingSand()
    {
        if (isLevelCompleted || isGameOver)
            return;

        bool anySandLeft = currentBlueSandCount > 0 ||
                           currentYellowSandCount > 0 ||
                           currentRedSandCount > 0 ||
                           currentGreenSandCount > 0 ||
                           currentOrangeSandCount > 0 ||
                           currentWhiteSandCount > 0 ||
                           currentBlackSandCount > 0;

        if (!anySandLeft)
        {
            isLevelCompleted = true;
            Debug.Log("All sand collected! Level complete.");
            ShowWinPanel();
        }
    }

    private void ShowWinPanel()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        isLevelCompleted = true;
        DisableGameplayForCompletion();

        onLevelWon?.Invoke();

        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }

        if (winPanel == null)
        {
            winPanel = GameObject.Find("WinPanel");
        }

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            var panelController = winPanel.GetComponent<LostPanelController>() ?? winPanel.GetComponentInChildren<LostPanelController>();
            panelController?.BindContinueButton(winPanel);
        }
        else
        {
            Debug.LogWarning("SandCollector: WinPanel is not assigned and no object named 'WinPanel' was found.");
        }

        if (LevelManager.Instance != null)
        {
            int activeBuildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            GameStats.MarkLevelCompleted(activeBuildIndex);
            GameStats.CurrentLevel = activeBuildIndex + 1;
        }

        Time.timeScale = 0f;
    }

    private void DisableGameplayForCompletion()
    {
        BucketSpawnerButton[] spawnerButtons = FindObjectsByType<BucketSpawnerButton>(FindObjectsSortMode.None);
        foreach (BucketSpawnerButton btn in spawnerButtons)
        {
            if (btn != null)
            {
                btn.DisableForGameOver();
            }
        }

        Bucket[] activeBuckets = FindObjectsByType<Bucket>(FindObjectsSortMode.None);
        foreach (Bucket bucket in activeBuckets)
        {
            if (bucket != null)
            {
                bucket.DisableForGameOver();
            }
        }

        ConveyorManager[] conveyors = FindObjectsByType<ConveyorManager>(FindObjectsSortMode.None);
        foreach (ConveyorManager cm in conveyors)
        {
            if (cm != null)
            {
                cm.StopSpawning();
                cm.enabled = false;
            }
        }
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
        isLevelCompleted = false;
        ResetLoseTimer();

        Texture2D readableTexture = MakeTextureReadable(levelTexture);
        Color32[] pixels = readableTexture.GetPixels32();
        int texW = levelTexture.width;
        int texH = levelTexture.height;

        int drawWidth = Mathf.Max(1, width - paddingLeft - paddingRight);
        int drawHeight = Mathf.Max(1, height - paddingBottom - paddingTop);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int cellType = EMPTY;
                Color32 pixel = COLOR_EMPTY;

                // Перевіряємо, чи піксель знаходиться всередині дозволеної області (з урахуванням відступів)
                if (x >= paddingLeft && x < width - paddingRight &&
                    y >= paddingBottom && y < height - paddingTop)
                {
                    int localX = x - paddingLeft;
                    int localY = y - paddingBottom;

                    int texX = Mathf.Clamp(Mathf.RoundToInt((float)localX / drawWidth * texW), 0, texW - 1);
                    int texY = Mathf.Clamp(Mathf.RoundToInt((float)localY / drawHeight * texH), 0, texH - 1);

                    pixel = pixels[texX + texY * texW];
                    cellType = ClassifyPixel(pixel);

                    if (IsGermanyFlagTexture(levelTexture.name) && pixel.a >= 50)
                    {
                        cellType = RemapGermanyFlagColor(pixel, cellType);
                    }
                }

                grid[x, y] = cellType;
                
                if (cellType == EMPTY)
                {
                    cellColors[x, y] = COLOR_EMPTY;
                }
                else
                {
                    cellColors[x, y] = pixel;
                }

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

        // --- Фільтрація шумів (кольори, яких менше noiseThreshold пікселів, замінюються на найпопулярніший) ---
        int maxCount = 0;
        int dominantColor = EMPTY;
        
        int[] counts = new int[] { 
            0, totalBlueSandCount, totalYellowSandCount, 0, totalRedSandCount, 
            totalGreenSandCount, totalOrangeSandCount, totalWhiteSandCount, totalBlackSandCount 
        };
        
        for (int i = 1; i <= 8; i++)
        {
            if (i == WALL) continue;
            if (counts[i] > maxCount)
            {
                maxCount = counts[i];
                dominantColor = i;
            }
        }

        if (dominantColor != EMPTY)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int cell = grid[x, y];
                    if (cell != EMPTY && cell != WALL && counts[cell] > 0 && counts[cell] < noiseThreshold)
                    {
                        // Віднімаємо від шумового
                        switch (cell)
                        {
                            case BLUE_SAND:   totalBlueSandCount--;   currentBlueSandCount--;   break;
                            case YELLOW_SAND: totalYellowSandCount--; currentYellowSandCount--; break;
                            case RED_SAND:    totalRedSandCount--;    currentRedSandCount--;    break;
                            case GREEN_SAND:  totalGreenSandCount--;  currentGreenSandCount--;  break;
                            case ORANGE_SAND: totalOrangeSandCount--; currentOrangeSandCount--; break;
                            case WHITE_SAND:  totalWhiteSandCount--;  currentWhiteSandCount--;  break;
                            case BLACK_SAND:  totalBlackSandCount--;  currentBlackSandCount--;  break;
                        }
                        
                        grid[x, y] = dominantColor;
                        
                        // Додаємо до домінантного
                        switch (dominantColor)
                        {
                            case BLUE_SAND:   
                                totalBlueSandCount++; currentBlueSandCount++; 
                                cellColors[x, y] = COLOR_BLUE; 
                                break;
                            case YELLOW_SAND: 
                                totalYellowSandCount++; currentYellowSandCount++; 
                                cellColors[x, y] = COLOR_YELLOW; 
                                break;
                            case RED_SAND:    
                                totalRedSandCount++; currentRedSandCount++; 
                                cellColors[x, y] = COLOR_RED; 
                                break;
                            case GREEN_SAND:  
                                totalGreenSandCount++; currentGreenSandCount++; 
                                cellColors[x, y] = COLOR_GREEN; 
                                break;
                            case ORANGE_SAND: 
                                totalOrangeSandCount++; currentOrangeSandCount++; 
                                cellColors[x, y] = COLOR_ORANGE; 
                                break;
                            case WHITE_SAND:  
                                totalWhiteSandCount++; currentWhiteSandCount++; 
                                cellColors[x, y] = COLOR_WHITE; 
                                break;
                            case BLACK_SAND:  
                                totalBlackSandCount++; currentBlackSandCount++; 
                                cellColors[x, y] = COLOR_BLACK; 
                                break;
                        }
                    }
                }
            }
        }
        // -----------------------------------------------------------------------------------------

        RemoveSmallWallIslands(minWallIslandSize);

        Debug.Log($"Рівень '{levelTexture.name}' завантажено: " +
                  $"синій={totalBlueSandCount}, жовтий={totalYellowSandCount}, " +
                  $"червоний={totalRedSandCount}, зелений={totalGreenSandCount}, " +
                  $"оранжевий={totalOrangeSandCount}, білий={totalWhiteSandCount}, чорний={totalBlackSandCount}");
    }

    private bool IsGermanyFlagTexture(string textureName)
    {
        return !string.IsNullOrEmpty(textureName) && textureName.Contains("photo_2026-07-16_00-36-38") ||
               !string.IsNullOrEmpty(textureName) && textureName.Contains("photo_2026-07-16_00-36-41") ||
               !string.IsNullOrEmpty(textureName) && textureName.Contains("photo_2026-07-16_00-36-42") ||
               !string.IsNullOrEmpty(textureName) && textureName.Contains("photo_2026-07-16_00-36-44") ||
               !string.IsNullOrEmpty(textureName) && textureName.Contains("photo_2026-07-16_00-36-45") ||
               !string.IsNullOrEmpty(textureName) && textureName.Contains("photo_2026-07-16_00-36-47") ||
               !string.IsNullOrEmpty(textureName) && textureName.Contains("photo_2026-07-16_00-36-48") ||
               !string.IsNullOrEmpty(textureName) && textureName.Contains("photo_2026-07-16_00-36-49");
    }

    private int RemapGermanyFlagColor(Color32 c, int defaultCellType)
    {
        if (defaultCellType != ORANGE_SAND)
            return defaultCellType;

        Color.RGBToHSV(c, out float h, out float s, out float v);
        if (s < 0.2f || v < 0.15f)
            return defaultCellType;

        if (h >= 0.05f && h < 0.09f)
            return RED_SAND;

        return defaultCellType;
    }

    private int ClassifyPixel(Color32 c)
    {
        // 1. Прозорий або майже прозорий → порожньо
        if (c.a < 50) return EMPTY;

        // Конвертуємо колір у формат HSV (Hue, Saturation, Value)
        Color.RGBToHSV(c, out float h, out float s, out float v);

        // 2. Чорний або дуже темний колір (знижено до 0.05, щоб темні відтінки не ставали чорними)
        if (v < 0.05f) return BLACK_SAND;

        // 3. Відтінки сірого (низька насиченість) (знижено до 0.1, щоб бліді відтінки кольорів працювали)
        if (s < 0.1f)
        {
            if (v > 0.72f) return WHITE_SAND; // Світло-сірий або білий
            return WALL; // Середньо-сірий - це стіна
        }

        // 4. Класифікація кольорових пікселів за відтінком (Hue)
        if (h < 0.05f || h > 0.90f) return RED_SAND;
        if (h >= 0.05f && h < 0.09f) return ORANGE_SAND;
        if (h >= 0.09f && h < 0.22f) return YELLOW_SAND;
        if (h >= 0.22f && h < 0.45f) return GREEN_SAND;
        if (h >= 0.45f && h <= 0.90f) return BLUE_SAND;

        return WALL; // Резервний варіант
    }

    /// <summary>
    /// Повертає readable копію текстури.
    /// Потрібно, якщо в Import Settings не увімкнено "Read/Write Enabled".
    /// </summary>
    private Texture2D MakeTextureReadable(Texture2D source)
    {
        // Спочатку намагаємось прочитати без копіювання
        try
        {
            source.GetPixels32();
            return source; // вже readable
        }
        catch { /* не readable — продовжуємо */ }

        // Копіюємо через RenderTexture
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width, source.height, 0,
            RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTexture = new Texture2D(source.width, source.height);
        readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTexture;
    }
}