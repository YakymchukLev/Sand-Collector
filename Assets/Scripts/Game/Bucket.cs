using UnityEngine;

public class Bucket : MonoBehaviour
{
    public int targetColorID; // Який колір має збирати (1 - синій, 2 - жовтий)
    public int capacity = 20; // Скільки піщинок вміщує
    public float speed = 2f;  // Швидкість руху конвеєра

    private int currentSandCount = 0;

    void Update()
    {
        // Відерце просто рухається ліворуч по конвеєру
        transform.Translate(Vector2.left * speed * Time.deltaTime);

        // Якщо відерце виїхало далеко за екран — видаляємо його
        if (transform.position.x < -10f)
        {
            Destroy(gameObject);
        }
    }

    // Метод, який викликає симуляція піску, коли піщинка падає у відерце
    public void AddSand(int colorID)
    {
        if (colorID == targetColorID)
        {
            currentSandCount++;
            Debug.Log($"Відерце {targetColorID} заповнено: {currentSandCount}/{capacity}");

            if (currentSandCount >= capacity)
            {
                OnBucketFull();
            }
        }
        else
        {
            Debug.Log("Неправильний колір піску!");
            // Тут можна додати штраф або візуальний ефект помилки
        }
    }

    void OnBucketFull()
    {
        Debug.Log("Відерце повне! Отримуємо бали.");
        // Додай анімацію зникнення або вивезення відерця
        Destroy(gameObject); 
    }
}