using UnityEngine;

public class BucketSpawnerButton : MonoBehaviour
{
    private Vector3 originalScale;
    [Header("Налаштування анімації кліку/наведення")]
    public float hoverScaleMultiplier = 1.1f;
    public float clickScaleMultiplier = 0.95f;
    public float animationSpeed = 10f;
    public float disableDuration = 5f; // Час, на який кнопка вимикається після використання
    
    [Header("Поведінка")]
    public bool reusable = false; // Якщо false, кнопка натискається лише один раз за рівень

    [Header("Налаштування кольору відерця")]
    public int bucketColorID = 1; // 1 - синій, 2 - жовтий

    [Header("Налаштування спрайту (опціонально)")]
    [Tooltip("Якщо перетягнути сюди спрайт, він замінить стандартний, і скрипт не буде автоматично перефарбовувати кнопку.")]
    public Sprite customButtonSprite;

    [Header("Налаштування місткості (опціонально)")]
    [Tooltip("Введіть кількість піску для відра. Якщо 0, місткість буде вираховуватись автоматично.")]
    public int customCapacity = 0;

    private Vector3 targetScale;
    private ConveyorManager conveyorManager;
    private bool isDisabled = false;
    private bool isGameOverDisabled = false;
    private SpriteRenderer[] buttonSpriteRenderers;
    private Collider2D buttonCollider;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        conveyorManager = FindObjectOfType<ConveyorManager>();
        buttonSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        buttonCollider = GetComponent<Collider2D>();

        if (buttonSpriteRenderers == null || buttonSpriteRenderers.Length == 0)
        {
            Debug.LogWarning($"[BucketSpawnerButton] У кнопки '{gameObject.name}' не знайдено SpriteRenderer. Переконайтеся, що він є на об'єкті або в дочірніх об'єктах.", this);
        }
        
        if (buttonCollider == null)
        {
            Debug.LogWarning($"[BucketSpawnerButton] На об'єкті '{gameObject.name}' немає Collider2D! Додайте його (наприклад, BoxCollider2D), щоб кліки мишкою працювали.", this);
        }

        ApplyButtonColor();
        
        // Перевіряємо наявність колайдера, необхідного для реєстрації кліків миші
        if (buttonCollider == null)
        {
            Debug.LogWarning($"[BucketSpawnerButton] На об'єкті '{gameObject.name}' немає Collider2D! Додайте його (наприклад, BoxCollider2D), щоб кліки мишкою працювали.", this);
        }

        if (customButtonSprite != null)
        {
            SpriteRenderer mainRenderer = GetComponent<SpriteRenderer>();
            if (mainRenderer != null)
            {
                mainRenderer.sprite = customButtonSprite;
                mainRenderer.color = Color.white; // Скидаємо відтінок
            }
        }

        ApplyButtonColor();
    }

    void Update()
    {
        // Плавна анімація зміни розміру (Lerp)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

        bool isColorInLevel = IsColorAvailableInLevel();
        bool shouldBeActive = isColorInLevel && !isDisabled && !isGameOverDisabled;

        if (buttonSpriteRenderers != null)
        {
            foreach (var renderer in buttonSpriteRenderers)
            {
                if (renderer == null) continue;
                renderer.enabled = shouldBeActive;
            }
        }

        if (buttonCollider != null)
        {
            buttonCollider.enabled = shouldBeActive;
        }

        if (shouldBeActive)
        {
            ApplyButtonColor();
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
        if (isDisabled || isGameOverDisabled || !IsColorAvailableInLevel())
            return;

        targetScale = originalScale * clickScaleMultiplier;
        
        bool spawned = false;
        if (conveyorManager != null)
        {
            spawned = conveyorManager.SpawnBucket(bucketColorID, customButtonSprite, customCapacity);
        }
        else
        {
            // Спробуємо знайти менеджер знову, якщо він не був ініціалізований раніше
            conveyorManager = FindObjectOfType<ConveyorManager>();
            if (conveyorManager != null)
            {
                spawned = conveyorManager.SpawnBucket(bucketColorID, customButtonSprite, customCapacity);
            }
            else
            {
                Debug.LogError("[BucketSpawnerButton] Не знайдено ConveyorManager на сцені!");
            }
        }
        
        if (spawned)
        {
            DisableButtonTemporarily();
        }
    }

    private void OnMouseUp()
    {
        if (isDisabled)
            return;

        // Повертаємо до масштабу наведення, якщо курсор все ще над об'єктом, інакше до початкового
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (buttonCollider != null && buttonCollider.OverlapPoint(mousePos))
        {
            targetScale = originalScale * hoverScaleMultiplier;
        }
        else
        {
            targetScale = originalScale;
        }
    }

    private void DisableButtonTemporarily()
    {
        isDisabled = true;
        if (buttonSpriteRenderers != null)
        {
            foreach (var renderer in buttonSpriteRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
        if (buttonCollider != null)
            buttonCollider.enabled = false;

        CancelInvoke(nameof(EnableButton));
        
        if (reusable)
        {
            Invoke(nameof(EnableButton), disableDuration);
        }
    }

    public void DisableForGameOver()
    {
        if (isGameOverDisabled)
            return;

        isGameOverDisabled = true;
        isDisabled = true;
        CancelInvoke(nameof(EnableButton));

        if (buttonSpriteRenderers != null)
        {
            foreach (var renderer in buttonSpriteRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        if (buttonCollider != null)
            buttonCollider.enabled = false;
    }

    private void EnableButton()
    {
        if (isGameOverDisabled)
            return;

        isDisabled = false;
        if (buttonSpriteRenderers != null)
        {
            foreach (var renderer in buttonSpriteRenderers)
            {
                if (renderer != null)
                    renderer.enabled = IsColorAvailableInLevel();
            }
        }
        if (buttonCollider != null)
            buttonCollider.enabled = IsColorAvailableInLevel();
    }

    public void ResetState()
    {
        CancelInvoke(nameof(EnableButton));
        EnableButton();
    }

    private bool IsColorAvailableInLevel()
    {
        if (SandCollector.Instance == null)
            return true;

        var activeColors = SandCollector.Instance.GetActiveSandColorIDs();
        return activeColors != null && activeColors.Contains(bucketColorID);
    }

    private void ApplyButtonColor()
    {
        if (buttonSpriteRenderers == null)
            return;

        // Якщо користувач призначив свій спрайт, вимикаємо автоматичне перефарбовування,
        // щоб не зіпсувати оригінальні кольори кастомної картинки.
        if (customButtonSprite != null)
            return;

        Color tintColor = GetColorForBucketID(bucketColorID);
        foreach (var renderer in buttonSpriteRenderers)
        {
            if (renderer != null)
            {
                renderer.color = tintColor;
            }
        }
    }

    private Color GetColorForBucketID(int colorID)
    {
        switch (colorID)
        {
            case 1: return new Color32(60, 120, 255, 255);
            case 2: return new Color32(255, 220, 70, 255);
            case 4: return new Color32(220, 40, 40, 255);
            case 5: return new Color32(40, 200, 60, 255);
            case 6: return new Color32(255, 140, 20, 255);
            case 7: return Color.white;
            case 8: return Color.black;
            default: return Color.white;
        }
    }
}
