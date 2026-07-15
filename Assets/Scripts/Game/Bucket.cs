using UnityEngine;

public class Bucket : MonoBehaviour
{
    public int targetColorID; // Який колір має збирати: 1=синій, 2=жовтий, 4=червоний, 5=зелений, 6=оранжевий
    public int capacity = 20;
    public float speed = 2f;

    public float destroyDelay = 1.5f;

    [Header("Спрайти відерця")]
    public Sprite blueBucketSprite;
    public Sprite yellowBucketSprite;
    public Sprite redBucketSprite;
    public Sprite greenBucketSprite;
    public Sprite orangeBucketSprite;

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

        // Зберігаємо оригінальний колір відерця (не змінюємо рандомно)
        SetCapacityFromSandCount();
        UpdateBucketVisuals();
    }

    public void SetCapacityFromSandCount()
    {
        if (SandCollector.Instance == null) return;

        switch (targetColorID)
        {
            case 1: capacity = Mathf.CeilToInt(SandCollector.totalBlueSandCount   / 3f); break;
            case 2: capacity = Mathf.CeilToInt(SandCollector.totalYellowSandCount / 3f); break;
            case 4: capacity = Mathf.CeilToInt(SandCollector.totalRedSandCount    / 3f); break;
            case 5: capacity = Mathf.CeilToInt(SandCollector.totalGreenSandCount  / 3f); break;
            case 6: capacity = Mathf.CeilToInt(SandCollector.totalOrangeSandCount / 3f); break;
        }
        if (capacity < 1) capacity = 1;
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
        if (sr == null) return;

        sr.color = Color.white;

        switch (targetColorID)
        {
            case 1: sr.sprite = blueBucketSprite;   break;
            case 2: sr.sprite = yellowBucketSprite; break;
            case 4: sr.sprite = redBucketSprite    != null ? redBucketSprite    : blueBucketSprite; sr.color = new Color32(220, 40,  40,  255); break;
            case 5: sr.sprite = greenBucketSprite  != null ? greenBucketSprite  : blueBucketSprite; sr.color = new Color32(40,  200, 60,  255); break;
            case 6: sr.sprite = orangeBucketSprite != null ? orangeBucketSprite : blueBucketSprite; sr.color = new Color32(255, 140, 20,  255); break;
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