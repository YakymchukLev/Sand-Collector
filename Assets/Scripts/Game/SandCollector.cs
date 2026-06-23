using UnityEngine;

public class SandCollector : MonoBehaviour
{
    [Header("Налаштування сітки")]
    public int width = 100;
    public int height = 100;

    public SandCollector sandCollector;
    
    [Header("Візуалізація")]
    public SpriteRenderer displayRenderer;

    private int[,] grid;
    private Texture2D texture;
    private Color32[] colorBuffer;

    // Ідентифікатори типів клітинок
    private const int EMPTY = 0;
    private const int BLUE_SAND = 1;
    private const int YELLOW_SAND = 2;
    private const int WALL = 3; // Новий тип для бортиків

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
                    // Залишаємо отвір по центру (шлюз) завширшки 10 пікселів, де стіни НЕ буде
                    if (x < width / 2 - 5 || x > width / 2 + 5)
                    {
                        grid[x, y] = WALL;
                    }
                }
            }
        }

        // 3. Генеруємо пісок ТІЛЬКИ всередині бортиків
        for (int x = wallThickness; x < width - wallThickness; x++)
        {
            for (int y = bottomY + wallThickness; y < height; y++)
            {
                if (y > 40 && y < 70)
                {
                    grid[x, y] = BLUE_SAND;
                }
                else if (y >= 70 && y < 90)
                {
                    grid[x, y] = YELLOW_SAND;
                }
            }
        }
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
                    // Перевіряємо, чи є зараз відерце в зоні тригера
                    if (sandCollector != null && sandCollector.currentBucket != null)
                    {
                        // Відерце є! Передаємо колір піску у відерце
                        sandCollector.currentBucket.AddSand(currentCell);

                        // Видаляємо піщинку з масиву (вона висипалася)
                        grid[x, y] = EMPTY;
                        continue;
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
                // 2. Осипання вниз-ліворуч
                else if (x > 0 && grid[x - 1, y - 1] == EMPTY)
                {
                    grid[x - 1, y - 1] = currentCell;
                    grid[x, y] = EMPTY;
                }
                // 3. Осипання вниз-праворуч
                else if (x < width - 1 && grid[x + 1, y - 1] == EMPTY)
                {
                    grid[x + 1, y - 1] = currentCell;
                    grid[x, y] = EMPTY;
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
}
