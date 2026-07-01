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

    private int currentBlueSandCount;
    private int currentYellowSandCount;
    private bool isGameOver = false;

    public static SandCollector Instance { get; private set; }
    public static int totalBlueSandCount { get; private set; }
    public static int totalYellowSandCount { get; private set; }

    private const int EMPTY = 0;
    private const int BLUE_SAND = 1;
    private const int YELLOW_SAND = 2;
    private const int WALL = 3; // Новий тип для бортиків

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        grid = new int[width, height];
        texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        displayRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        
        colorBuffer = new Color32[width * height];

        BuildBordersAndSand();
    }

    void Update()
    {
        UpdateSandPhysics();
        DrawGrid();
    }

    // Більше не використовуємо тригери, оскільки відра детектуються по всій ширині динамічно

    // Створюємо бортики та засипаємо пісок всередину
    void BuildBordersAndSand()
    {
        // 1. Спочатку робимо все пустим
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = EMPTY;
            }
        }

        // 2. Будуємо бортики (стіни)
        int wallThickness = 4; // Товщина бортика в пікселях
        int bottomY = 10;      // На якій висоті від низу буде дно коробки

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Лівий бортик
                if (x < wallThickness) grid[x, y] = WALL;
                
                // Правий бортик
                if (x >= width - wallThickness) grid[x, y] = WALL;

                // Нижні бортики (дно рами)
                if (y >= bottomY && y < bottomY + wallThickness)
                {
                    // Залишаємо отвір по центру (шлюз) завширшки gateWidth пікселів, де стіни НЕ буде
                    int halfGate = gateWidth / 2;
                    if (x < width / 2 - halfGate || x > width / 2 + halfGate)
                    {
                        grid[x, y] = WALL;
                    }
                }
            }
        }

        totalBlueSandCount = 0;
        totalYellowSandCount = 0;

        // 3. Генеруємо пісок ТІЛЬКИ всередині бортиків
        for (int x = wallThickness; x < width - wallThickness; x++)
        {
            for (int y = bottomY + wallThickness; y < height; y++)
            {
                if (y > 40 && y < 70)
                {
                    grid[x, y] = BLUE_SAND;
                    totalBlueSandCount++;
                }
                else if (y >= 70 && y < 90)
                {
                    grid[x, y] = YELLOW_SAND;
                    totalYellowSandCount++;
                }
            }
        }

        currentBlueSandCount = totalBlueSandCount;
        currentYellowSandCount = totalYellowSandCount;
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

            // Працюємо тільки з синім або жовтим піском
            if (currentCell == BLUE_SAND || currentCell == YELLOW_SAND)
            {
                // -------------------------------------------------------------
                // ЛОГІКА РОБОТИ КЛАПАНА/ШЛЮЗУ (y == 10)
                // -------------------------------------------------------------
                if (y == 10)
                {
                    // Шукаємо відерце безпосередньо під цим стовпчиком x
                    Bucket bucketAtPos = GetBucketAtGridX(x);
                    if (bucketAtPos != null)
                    {
                        // Передаємо колір піску у відерце. Якщо колір співпав, воно поверне true
                        if (bucketAtPos.AddSand(currentCell))
                        {
                            // Тільки якщо колір співпав, видаляємо піщинку з сітки (вона засипалася у відерце)
                            grid[x, y] = EMPTY;

                            // Зменшуємо лічильник відповідного піску
                            if (currentCell == BLUE_SAND)
                            {
                                currentBlueSandCount--;
                            }
                            else if (currentCell == YELLOW_SAND)
                            {
                                currentYellowSandCount--;
                            }

                            // Якщо на полі більше не залишилося піску цього кольору, примусово заповнюємо відерце до 100%
                            if (!IsAnySandOfColorLeft(currentCell))
                            {
                                bucketAtPos.ForceFull();
                            }

                            CheckRemainingSand();
                            continue;
                        }
                        else
                        {
                            // Якщо колір не співпав, шлюз залишається закритим для цієї піщинки (вона не висипається)
                            continue;
                        }
                    }
                    else
                    {
                        // ВІДЕРЦЯ НЕМАЄ! Шлюз закритий.
                        // Ми просто пропускаємо рух цієї піщинки. Вона залишається на місці (y = 10)
                        // і не пускає верхній пісок вивалитися назовні.
                        continue; 
                    }
                }

                // -------------------------------------------------------------
                // СТАНДАРТНИЙ РУХ ПІСКУ ВСЕРЕДИНІ РАМИ
                // -------------------------------------------------------------
                // 1. Рух прямо вниз
                if (grid[x, y - 1] == EMPTY)
                {
                    grid[x, y - 1] = currentCell;
                    grid[x, y] = EMPTY;
                }
                // 2. Рандомізоване осипання вбік (ліворуч або праворуч)
                else
                {
                    bool checkLeftFirst = Random.value < 0.5f;
                    if (checkLeftFirst)
                    {
                        if (x > 0 && grid[x - 1, y - 1] == EMPTY)
                        {
                            grid[x - 1, y - 1] = currentCell;
                            grid[x, y] = EMPTY;
                        }
                        else if (x < width - 1 && grid[x + 1, y - 1] == EMPTY)
                        {
                            grid[x + 1, y - 1] = currentCell;
                            grid[x, y] = EMPTY;
                        }
                    }
                    else
                    {
                        if (x < width - 1 && grid[x + 1, y - 1] == EMPTY)
                        {
                            grid[x + 1, y - 1] = currentCell;
                            grid[x, y] = EMPTY;
                        }
                        else if (x > 0 && grid[x - 1, y - 1] == EMPTY)
                        {
                            grid[x - 1, y - 1] = currentCell;
                            grid[x, y] = EMPTY;
                        }
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
                    case EMPTY:
                        colorBuffer[pixelIndex] = Color.clear; // Прозоре тло
                        break;
                    case BLUE_SAND:
                        colorBuffer[pixelIndex] = Color.blue;
                        break;
                    case YELLOW_SAND:
                        colorBuffer[pixelIndex] = Color.yellow;
                        break;
                    case WALL:
                        colorBuffer[pixelIndex] = Color.gray; // Бортики будуть сірими
                        break;
                }
            }
        }

        texture.SetPixels32(colorBuffer);
        texture.Apply();
    }

    Bucket GetBucketAtGridX(int x)
    {
        // Отримуємо межі спрайту у світових координатах
        Bounds bounds = displayRenderer.bounds;
        float spriteWorldWidth = bounds.size.x;

        // Рахуємо точну світову координату X для цього стовпчика сітки
        // x / (float)width дає значення від 0.0 до 1.0
        float worldX = bounds.min.x + ((float)x / width) * spriteWorldWidth;

        // Шукаємо відерце під цією світовою координатою X
        Bucket[] buckets = FindObjectsOfType<Bucket>();
        foreach (Bucket bucket in buckets)
        {
            Collider2D bucketCollider = bucket.GetComponent<Collider2D>();
            if (bucketCollider != null)
            {
                if (worldX >= bucketCollider.bounds.min.x && worldX <= bucketCollider.bounds.max.x)
                {
                    return bucket;
                }
            }
            else
            {
                if (Mathf.Abs(bucket.transform.position.x - worldX) < 0.5f)
                {
                    return bucket;
                }
            }
        }
        return null;
    }

    public bool IsAnySandOfColorLeft(int colorID)
    {
        if (colorID == BLUE_SAND) return currentBlueSandCount > 0;
        if (colorID == YELLOW_SAND) return currentYellowSandCount > 0;
        return false;
    }

    private void CheckRemainingSand()
    {
        if (!isGameOver && currentBlueSandCount <= 0 && currentYellowSandCount <= 0)
        {
            isGameOver = true;
            Debug.Log("Весь пісок зібрано! Повертаємось у меню за 2 секунди...");
            Invoke(nameof(LoadMenuScene), 2f);
        }
    }

    private void LoadMenuScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}
