using UnityEngine;

public class Bucket : MonoBehaviour
{
    public int targetColorID; // Який колір має збирати (1 - синій, 2 - жовтий)
    public int capacity = 20; // Скільки піщинок вміщує
    public float speed = 2f;  // Швидкість руху конвеєра

    public float destroyDelay = 1.5f; // Затримка перед видаленням відерця в секундах

    [Header("Спрайти відерця")]
    public Sprite blueBucketSprite;
    public Sprite yellowBucketSprite;

    private int currentSandCount = 0;
    private Vector3 startPosition;
    private bool isFull = false;

    void Start()
    {
        startPosition = transform.position;
        SetCapacityFromSandCount();
    }

    void Update()
    {
        // Відерце просто рухається ліворуч по конвеєру
        transform.Translate(Vector2.left * speed * Time.deltaTime);

        // Якщо відерце досягло кінцевої точки (і воно не повне) — повертаємо його на початок
        if (!isFull && transform.position.x < -2.5f)
        {
            ResetBucket();
        }
    }

    void ResetBucket()
    {
        transform.position = startPosition;
        currentSandCount = 0;
        isFull = false;

        // Рандомно змінюємо колір при поверненні на початок
        targetColorID = Random.Range(1, 3);
        SetCapacityFromSandCount();
        UpdateBucketVisuals();
    }

    public void SetCapacityFromSandCount()
    {
        if (SandCollector.Instance == null) return;

        if (targetColorID == 1) // Синій
        {
            capacity = Mathf.CeilToInt(SandCollector.totalBlueSandCount / 3f);
        }
        else if (targetColorID == 2) // Жовтий
        {
            capacity = Mathf.CeilToInt(SandCollector.totalYellowSandCount / 3f);
        }
    }

    public void ForceFull()
    {
        if (isFull) return;
        isFull = true;
        currentSandCount = capacity; // Задаємо 100% заповненість
        OnBucketFull();
    }

    public void UpdateBucketVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Скидаємо колір-маску на білий, щоб не було зеленого відтінку від перекриття
            sr.color = Color.white;

            if (targetColorID == 1)
            {
                sr.sprite = blueBucketSprite;
            }
            else if (targetColorID == 2)
            {
                sr.sprite = yellowBucketSprite;
            }
        }
    }

    public float GetFillPercentage()
    {
        return Mathf.Clamp((currentSandCount / (float)capacity) * 100f, 0f, 100f);
    }

    // Метод, який викликає симуляція піску, коли піщинка падає у відерце
    public bool AddSand(int colorID)
    {
        if (isFull) return false;

        if (colorID == targetColorID)
        {
            currentSandCount++;
            float percentage = GetFillPercentage();
            Debug.Log($"Відерце {targetColorID} заповнено: {percentage:F0}% ({currentSandCount}/{capacity})");

            if (currentSandCount >= capacity)
            {
                isFull = true;
                OnBucketFull();
            }
            return true;
        }
        else
        {
            Debug.Log("Неправильний колір піску!");
            // Тут можна додати штраф або візуальний ефект помилки
            return false;
        }
    }

    void OnBucketFull()
    {
        Debug.Log("Відерце повне (100%)! Отримуємо бали.");
        // Знищуємо об'єкт із затримкою
        Destroy(gameObject, destroyDelay); 
    }
}