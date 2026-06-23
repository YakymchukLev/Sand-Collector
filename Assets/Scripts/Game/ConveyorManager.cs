using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public GameObject bucketPrefab; // Сюди перетягни префаб відерця
    public Transform spawnPoint;    // Точка спавну (справа за екраном)
    public float spawnInterval = 3f; // Наскільки часто виїжджають відерця

    void Start()
    {
        // Запускаємо регулярний спавн відерець
        InvokeRepeating(nameof(SpawnBucket), 0f, spawnInterval);
    }

    void SpawnBucket()
    {
        GameObject newBucket = Instantiate(bucketPrefab, spawnPoint.position, Quaternion.identity);
        
        // Рандомно задаємо колір для нового відерця (1 - синій, 2 - жовтий)
        Bucket bucketScript = newBucket.GetComponent<Bucket>();
        bucketScript.targetColorID = Random.Range(1, 3); 

        // Для наочності міняємо колір самого спрайту відерця
        SpriteRenderer sr = newBucket.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = (bucketScript.targetColorID == 1) ? Color.blue : Color.yellow;
        }
    }
}