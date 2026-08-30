using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class BucketCount : MonoBehaviour
{
    private TextMeshProUGUI countText;
    private ConveyorManager conveyor;

    void Start()
    {
        countText = GetComponent<TextMeshProUGUI>();
        conveyor = FindAnyObjectByType<ConveyorManager>();
    }

    void Update()
    {
        if (countText != null && conveyor != null)
        {
            int currentBuckets = Bucket.AllBuckets.Count;
            countText.text = $"{currentBuckets}/{conveyor.maxActiveBuckets}";
        }
    }
}
