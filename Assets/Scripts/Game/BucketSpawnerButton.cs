using UnityEngine;

public class BucketSpawnerButton : MonoBehaviour
{
    private Vector3 originalScale;
    [Header("Налаштування анімації кліку/наведення")]
    public float hoverScaleMultiplier = 1.1f;
    public float clickScaleMultiplier = 0.95f;
    public float animationSpeed = 10f;

    [Header("Налаштування кольору відерця")]
    public int bucketColorID = 1; // 1 - синій, 2 - жовтий

    private Vector3 targetScale;
    private ConveyorManager conveyorManager;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        conveyorManager = FindObjectOfType<ConveyorManager>();
        
        // Автоматично визначаємо колір за спрайтом кнопки
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            string spriteName = sr.sprite.name.ToLower();
            if (spriteName.Contains("blue"))
            {
                bucketColorID = 1;
            }
            else if (spriteName.Contains("yellow"))
            {
                bucketColorID = 2;
            }
            else if (spriteName.Contains("red"))
            {
                bucketColorID = 4;
            }
            else if (spriteName.Contains("green"))
            {
                bucketColorID = 5;
            }
            else if (spriteName.Contains("orange"))
            {
                bucketColorID = 6;
            }
            else if (spriteName.Contains("white"))
            {
                bucketColorID = 7;
            }
            else if (spriteName.Contains("black"))
            {
                bucketColorID = 8;
            }
        }
        
        // Перевіряємо наявність колайдера, необхідного для реєстрації кліків миші
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning($"[BucketSpawnerButton] На об'єкті '{gameObject.name}' немає Collider2D! Додайте його (наприклад, BoxCollider2D), щоб кліки мишкою працювали.", this);
        }
    }

    void Update()
    {
        // Плавна анімація зміни розміру (Lerp)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

        // Відображаємо кнопку спавну лише тоді, коли цей колір є на картинці поточного рівня
        if (SandCollector.Instance != null)
        {
            var activeColors = SandCollector.Instance.GetActiveSandColorIDs();
            bool isColorInLevel = activeColors.Contains(bucketColorID);
            
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            Collider2D col = GetComponent<Collider2D>();
            if (sr != null) sr.enabled = isColorInLevel;
            if (col != null) col.enabled = isColorInLevel;
        }
    }

    private void OnMouseEnter()
    {
        targetScale = originalScale * hoverScaleMultiplier;
    }

    private void OnMouseExit()
    {
        targetScale = originalScale;
    }

    private void OnMouseDown()
    {
        targetScale = originalScale * clickScaleMultiplier;
        
        bool spawned = false;
        if (conveyorManager != null)
        {
            spawned = conveyorManager.SpawnBucket(bucketColorID);
        }
        else
        {
            // Спробуємо знайти менеджер знову, якщо він не був ініціалізований раніше
            conveyorManager = FindObjectOfType<ConveyorManager>();
            if (conveyorManager != null)
            {
                spawned = conveyorManager.SpawnBucket(bucketColorID);
            }
            else
            {
                Debug.LogError("[BucketSpawnerButton] Не знайдено ConveyorManager на сцені!");
            }
        }
        
        if (spawned)
        {
            Destroy(gameObject);
        }
    }

    private void OnMouseUp()
    {
        // Повертаємо до масштабу наведення, якщо курсор все ще над об'єктом, інакше до початкового
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.OverlapPoint(mousePos))
        {
            targetScale = originalScale * hoverScaleMultiplier;
        }
        else
        {
            targetScale = originalScale;
        }
    }
}
