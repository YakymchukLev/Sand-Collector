using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public GameObject bucketPrefab; // Сюди перетягни префаб відерця
    public Transform spawnPoint;    // Точка спавну (справа за екраном)
    public float spawnInterval = 3f; // Наскільки часто виїжджають відерця
    public float minDistanceBetweenBuckets = 1.5f; // Мінімальна відстань між відерцями
    public int maxActiveBuckets = 4; // Максимальна кількість відер на сцені одночасно
    public GameObject tooManyBucketsWarningText; // UI-повідомлення, яке з'являється при перевищенні ліміту
    public float warningDuration = 2f;

    [Header("Автоматичний спавн")]
    public bool autoSpawn = false; // Якщо true, відерця спавняться самі. Якщо false - тільки при натисканні.

    void Awake()
    {
        maxActiveBuckets = Mathf.Max(4, maxActiveBuckets);
    }

    void Start()
    {
        minDistanceBetweenBuckets = 1.5f; // Обмежуємо відстань кодом, щоб оминути застаріле значення в сцені
        if (bucketPrefab == null)
        {
            Debug.LogError("[ConveyorManager] bucketPrefab не задано. Встановіть префаб відерця в інспекторі.", this);
        }
        if (spawnPoint == null)
        {
            Debug.LogError("[ConveyorManager] spawnPoint не задано. Встановіть точку спавну в інспекторі.", this);
        }

        if (autoSpawn)
        {
            // Запускаємо регулярний спавн відерець
            InvokeRepeating(nameof(SpawnBucket), 0f, spawnInterval);
        }
    }

    void OnDisable()
    {
        StopSpawning();
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(SpawnBucket));
    }

    public void SpawnBucket()
    {
        if (SandCollector.Instance != null)
        {
            var activeColors = SandCollector.Instance.GetActiveSandColorIDs();
            if (activeColors != null && activeColors.Count > 0)
            {
                // Обираємо тільки з кольорів, які присутні на фото поточного рівня!
                int randomColorID = activeColors[Random.Range(0, activeColors.Count)];
                SpawnBucket(randomColorID);
                return;
            }
        }
        SpawnBucket(1);
    }

    public bool SpawnBucket(int colorID, Sprite customSprite = null, int customCapacity = 0)
    {
        if (customSprite == null || customCapacity <= 0)
        {
            BucketSpawnerButton[] buttons = FindObjectsByType<BucketSpawnerButton>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (btn.bucketColorID == colorID)
                {
                    if (customSprite == null && btn.customButtonSprite != null)
                        customSprite = btn.customButtonSprite;
                    
                    if (customCapacity <= 0 && btn.customCapacity > 0)
                        customCapacity = btn.customCapacity;
                }
            }
        }

        // Перевірка кількості активних відер на сцені
        Bucket[] activeBuckets = FindObjectsByType<Bucket>(FindObjectsSortMode.None);
        if (activeBuckets.Length >= maxActiveBuckets)
        {
            ShowTooManyBucketsWarning();
            return false;
        }

        // Перевірка відстані до найближчого відерця від точки спавну
        foreach (Bucket b in activeBuckets)
        {
            if (Vector3.Distance(b.transform.position, spawnPoint.position) < minDistanceBetweenBuckets)
            {
                return false; // Якщо якесь відерце занадто близько до точки спавну, не спавнимо
            }
        }

        if (bucketPrefab == null)
        {
            Debug.LogError("[ConveyorManager] Неможливо спавнити відро: bucketPrefab не задано.");
            return false;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("[ConveyorManager] Неможливо спавнити відро: spawnPoint не задано.");
            return false;
        }

        GameObject newBucket = Instantiate(bucketPrefab, spawnPoint.position, Quaternion.identity);
        if (newBucket == null)
        {
            Debug.LogError("[ConveyorManager] Не вдалося створити екземпляр bucketPrefab.");
            return false;
        }

        // Задаємо колір для нового відерця
        Bucket bucketScript = newBucket.GetComponent<Bucket>();
        if (bucketScript == null)
        {
            Debug.LogError("[ConveyorManager] Префаб відра не містить компонент Bucket.", newBucket);
            Destroy(newBucket);
            return false;
        }

        bucketScript.SetTargetColorID(colorID, false, customSprite, customCapacity);

        return true;
    }

    private void ShowTooManyBucketsWarning()
    {
        if (tooManyBucketsWarningText == null)
            return;

        tooManyBucketsWarningText.SetActive(true);
        CancelInvoke(nameof(HideTooManyBucketsWarning));
        Invoke(nameof(HideTooManyBucketsWarning), warningDuration);
    }

    private void HideTooManyBucketsWarning()
    {
        if (tooManyBucketsWarningText == null)
            return;

        tooManyBucketsWarningText.SetActive(false);
    }
}