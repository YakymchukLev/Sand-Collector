using UnityEngine;

public class BucketSpawnerButton : MonoBehaviour
{
    private Vector3 originalScale;
    [Header("Налаштування анімації кліку/наведення")]
    public float hoverScaleMultiplier = 1.1f;
    public float clickScaleMultiplier = 0.95f;
    public float animationSpeed = 10f;

    private Vector3 targetScale;
    private ConveyorManager conveyorManager;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        conveyorManager = FindObjectOfType<ConveyorManager>();
        
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
        
        if (conveyorManager != null)
        {
            conveyorManager.SpawnBucket();
        }
        else
        {
            // Спробуємо знайти менеджер знову, якщо він не був ініціалізований раніше
            conveyorManager = FindObjectOfType<ConveyorManager>();
            if (conveyorManager != null)
            {
                conveyorManager.SpawnBucket();
            }
            else
            {
                Debug.LogError("[BucketSpawnerButton] Не знайдено ConveyorManager на сцені!");
            }
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
