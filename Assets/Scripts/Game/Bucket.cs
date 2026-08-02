using UnityEngine;

public class Bucket : MonoBehaviour
{
    public int targetColorID; // Який колір має збирати: 1=синій, 2=жовтий, 4=червоний, 5=зелений, 6=оранжевий
    public int capacity = 20;
    public float capacityDivisor = 3f; // Велике значення = менший об'єм у відрі
    public float speed = 2f;
    public float minBucketSpacing = 1.5f; // Мінімальна відстань між відерцями по конвеєру

    public float destroyDelay = 1.5f;
    public float returnDelay = 4f;

    [Header("Спрайти відерця")]
    public Sprite blueBucketSprite;
    public Sprite yellowBucketSprite;
    public Sprite redBucketSprite;
    public Sprite greenBucketSprite;
    public Sprite orangeBucketSprite;
    public Sprite whiteBucketSprite;
    public Sprite blackBucketSprite;
    [Tooltip("Фолбек-спрайт для відерця, якщо конкретний колір не має призначеного спрайту")]
    public Sprite defaultBucketSprite;

    private Sprite customSpriteOverride;
    private int customCapacityOverride = 0;
    private int currentSandCount = 0;
    // Expose bucket fullness state for lose condition
    public bool IsFull => isFull;
    private Vector3 startPosition;
    private bool isFull = false;
    private bool isGameOverDisabled = false;

    void Start()
    {
        startPosition = transform.position;
        SetCapacityFromSandCount();
        UpdateBucketVisuals();
    }

    void Update()
    {
        if (isGameOverDisabled)
            return;

        if (SandCollector.Instance != null && !SandCollector.Instance.IsAnySandOfColorLeft(targetColorID))
        {
            ForceFull();
            return;
        }

        // Відерце рухається ліворуч по конвеєру, але не наштовхується на інше відерце попереду
        Vector2 movement = Vector2.left * speed * Time.deltaTime;
        Vector3 targetPosition = transform.position + (Vector3)movement;

        if (IsBucketTooCloseAhead(targetPosition))
        {
            return;
        }

        transform.position = targetPosition;

        // Якщо відерце проїхало весь конвеєр (за межі екрану), воно одразу повертається на початок
        if (transform.position.x < -2.5f)
        {
            ReturnToStart();
        }
    }

    private bool IsBucketTooCloseAhead(Vector3 targetPosition)
    {
        Bucket[] buckets = FindObjectsByType<Bucket>(FindObjectsSortMode.None);
        foreach (Bucket bucket in buckets)
        {
            if (bucket == this) continue;
            if (bucket.transform.position.x >= transform.position.x) continue;

            float deltaX = transform.position.x - bucket.transform.position.x;
            if (deltaX < minBucketSpacing)
            {
                return true;
            }
        }
        return false;
    }

    public void SetCapacityFromSandCount()
    {
        if (customCapacityOverride > 0)
        {
            capacity = customCapacityOverride;
            return;
        }

        if (SandCollector.Instance == null) return;

        // Подвоюємо дільник, щоб об'єм відра став удвічі меншим
        float effectiveDivisor = capacityDivisor * 2f;

        switch (targetColorID)
        {
            case 1: capacity = Mathf.CeilToInt(SandCollector.totalBlueSandCount   / effectiveDivisor); break;
            case 2: capacity = Mathf.CeilToInt(SandCollector.totalYellowSandCount / effectiveDivisor); break;
            case 4: capacity = Mathf.CeilToInt(SandCollector.totalRedSandCount    / effectiveDivisor); break;
            case 5: capacity = Mathf.CeilToInt(SandCollector.totalGreenSandCount  / effectiveDivisor); break;
            case 6: capacity = Mathf.CeilToInt(SandCollector.totalOrangeSandCount / effectiveDivisor); break;
            case 7: capacity = Mathf.CeilToInt(SandCollector.totalWhiteSandCount  / effectiveDivisor); break;
            case 8: capacity = Mathf.CeilToInt(SandCollector.totalBlackSandCount  / effectiveDivisor); break;
        }
        if (capacity < 1) capacity = 1;
    }

    public void DisableForGameOver()
    {
        if (isGameOverDisabled)
            return;

        isGameOverDisabled = true;
        CancelInvoke();
        enabled = false;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        SpriteRenderer sr = GetBucketSpriteRenderer();
        if (sr != null)
        {
            sr.enabled = false;
        }
    }

    public void RemoveFromScene(bool immediate = false)
    {
        if (gameObject == null) return;

        enabled = false;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        gameObject.SetActive(false);
        Destroy(gameObject, immediate ? 0f : destroyDelay);
    }

    private void ReturnToStart()
    {
        transform.position = startPosition;
        UpdateBucketVisuals();
    }

    public void ForceFull()
    {
        if (isFull) return;
        isFull = true;
        currentSandCount = capacity;
        OnBucketFull();
    }

    private SpriteRenderer GetBucketSpriteRenderer()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }
        return sr;
    }

    private Color GetBucketColorForTarget()
    {
        switch (targetColorID)
        {
            case 1: return new Color32(60, 120, 255, 255);  // синій
            case 2: return new Color32(255, 220, 70, 255); // жовтий
            case 4: return new Color32(220, 40, 40, 255);  // червоний
            case 5: return new Color32(40, 200, 60, 255);  // зелений
            case 6: return new Color32(255, 140, 20, 255); // помаранчевий
            case 7: return Color.white;                    // білий
            case 8: return Color.black;                    // чорний
            default: return Color.white;
        }
    }

    public void UpdateBucketVisuals()
    {
        SpriteRenderer sr = GetBucketSpriteRenderer();
        if (sr == null) return;

        sr.enabled = true;

        if (customSpriteOverride != null)
        {
            sr.sprite = customSpriteOverride;
            sr.color = Color.white;
            return;
        }

        Sprite selectedSprite = null;
        switch (targetColorID)
        {
            case 1: selectedSprite = blueBucketSprite; break;
            case 2: selectedSprite = yellowBucketSprite; break;
            case 4: selectedSprite = redBucketSprite; break;
            case 5: selectedSprite = greenBucketSprite; break;
            case 6: selectedSprite = orangeBucketSprite; break;
            case 7: selectedSprite = whiteBucketSprite; break;
            case 8: selectedSprite = blackBucketSprite; break;
        }

        sr.sprite = selectedSprite ?? defaultBucketSprite ?? sr.sprite;
        if (sr.sprite == null)
        {
            Debug.LogWarning("Bucket.UpdateBucketVisuals: неможливо встановити спрайт для відра, бо відсутні відповідний та фолбек-спрайти.", this);
        }

        sr.color = GetBucketColorForTarget();
    }

    public void SetTargetColorID(int colorID, bool overrideExisting = false, Sprite customSprite = null, int customCapacity = 0)
    {
        if (!overrideExisting && targetColorID != 0)
            return;

        targetColorID = colorID;
        customSpriteOverride = customSprite;
        customCapacityOverride = customCapacity;
        SetCapacityFromSandCount();
        UpdateBucketVisuals();
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
        CancelInvoke();
        RemoveFromScene();
    }
}