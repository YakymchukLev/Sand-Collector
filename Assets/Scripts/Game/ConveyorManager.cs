using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public GameObject bucketPrefab; // Сюди перетягни префаб відерця
    public Transform spawnPoint;    // Точка спавну (справа за екраном)
    public float spawnInterval = 3f; // Наскільки часто виїжджають відерця
    public float minDistanceBetweenBuckets = 4f; // Мінімальна відстань між відерцями

    [Header("Автоматичний спавн")]
    public bool autoSpawn = false; // Якщо true, відерця спавняться самі. Якщо false - тільки при натисканні.

    void Start()
    {
        if (autoSpawn)
        {
            // Запускаємо регулярний спавн відерець
            InvokeRepeating(nameof(SpawnBucket), 0f, spawnInterval);
        }
    }

    public void SpawnBucket()
    {
        // Перевірка кількості відер на конвеєрі
        Bucket[] activeBuckets = FindObjectsOfType<Bucket>();
        if (activeBuckets.Length >= 3)
        {
            return;
        }

        // Перевірка відстані до найближчого відерця від точки спавну
        foreach (Bucket b in activeBuckets)
        {
            if (Vector3.Distance(b.transform.position, spawnPoint.position) < minDistanceBetweenBuckets)
            {
                return; // Якщо якесь відерце занадто близько до точки спавну, не спавнимо
            }
        }

        GameObject newBucket = Instantiate(bucketPrefab, spawnPoint.position, Quaternion.identity);
        
        // Рандомно задаємо колір для нового відерця (1 - синій, 2 - жовтий)
        Bucket bucketScript = newBucket.GetComponent<Bucket>();
        bucketScript.targetColorID = Random.Range(1, 3); 
        bucketScript.SetCapacityFromSandCount();
        bucketScript.UpdateBucketVisuals();
    }
}