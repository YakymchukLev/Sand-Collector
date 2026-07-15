using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public GameObject bucketPrefab; // Сюди перетягни префаб відерця
    public Transform spawnPoint;    // Точка спавну (справа за екраном)
    public float spawnInterval = 3f; // Наскільки часто виїжджають відерця
    public float minDistanceBetweenBuckets = 1.5f; // Мінімальна відстань між відерцями

    [Header("Автоматичний спавн")]
    public bool autoSpawn = false; // Якщо true, відерця спавняться самі. Якщо false - тільки при натисканні.

    void Start()
    {
        minDistanceBetweenBuckets = 1.5f; // Обмежуємо відстань кодом, щоб оминути застаріле значення в сцені
        if (autoSpawn)
        {
            // Запускаємо регулярний спавн відерець
            InvokeRepeating(nameof(SpawnBucket), 0f, spawnInterval);
        }
    }

    public void SpawnBucket()
    {
        SpawnBucket(Random.Range(1, 3));
    }

    public bool SpawnBucket(int colorID)
    {
        // Перевірка кількості відер на конвеєрі (збільшуємо ліміт до 6, оскільки нам потрібно по 3 кожного кольору)
        Bucket[] activeBuckets = FindObjectsOfType<Bucket>();
        if (activeBuckets.Length >= 6)
        {
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

        GameObject newBucket = Instantiate(bucketPrefab, spawnPoint.position, Quaternion.identity);
        
        // Задаємо колір для нового відерця
        Bucket bucketScript = newBucket.GetComponent<Bucket>();
        bucketScript.targetColorID = colorID; 
        bucketScript.SetCapacityFromSandCount();
        bucketScript.UpdateBucketVisuals();

        return true;
    }
}