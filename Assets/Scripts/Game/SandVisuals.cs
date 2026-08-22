using UnityEngine;
using System.Collections.Generic;

public class SandVisuals : MonoBehaviour
{
    public static SandVisuals Instance;

    [Header("???????????? ???????")]
    [Tooltip("??????? ??????? ???????, ?? ?????? ? ?????. ???????, ???? ???? ??????? ??????.")]
    public float particleSizeMultiplier = 0.4f;

    private class VisualParticle
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Vector3 startPos;
        public Transform target;
        public float progress;
        public float speed;
        public bool active;
        public Vector3 controlPoint;
    }

    private List<VisualParticle> pool = new List<VisualParticle>();
    private Sprite squareSprite;

    void Awake()
    {
        Instance = this;
        
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
    }

    public void SpawnFlyingSand(Vector3 startWorldPos, Transform bucketTransform, Color32 color, float sizeScale)
    {
        VisualParticle p = GetParticle();
        p.startPos = startWorldPos;
        p.target = bucketTransform;
        p.progress = 0f;
        p.speed = Random.Range(3.5f, 6.0f); 

        float offsetX = Random.Range(-0.4f, 0.4f);
        float offsetY = Random.Range(-0.1f, 0.3f);
        p.controlPoint = startWorldPos + (bucketTransform.position - startWorldPos) * 0.4f;
        p.controlPoint.x += offsetX;
        p.controlPoint.y += offsetY;

        p.sr.color = color;
        p.go.transform.position = startWorldPos;
        
        float finalSize = sizeScale * particleSizeMultiplier;
        p.go.transform.localScale = new Vector3(finalSize, finalSize, 1f);
        
        p.go.SetActive(true);
        p.active = true;
    }

    private VisualParticle GetParticle()
    {
        foreach (var p in pool)
        {
            if (!p.active) return p;
        }

        VisualParticle newP = new VisualParticle();
        newP.go = new GameObject("SandVisualParticle");
        newP.go.transform.SetParent(transform);
        newP.sr = newP.go.AddComponent<SpriteRenderer>();
        newP.sr.sprite = squareSprite;
        newP.sr.sortingOrder = 100;
        
        newP.active = false;
        newP.go.SetActive(false);
        pool.Add(newP);
        return newP;
    }

    void Update()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            var p = pool[i];
            if (!p.active) continue;

            if (p.target == null || !p.target.gameObject.activeInHierarchy)
            {
                p.active = false;
                p.go.SetActive(false);
                continue;
            }

            p.progress += Time.deltaTime * p.speed;
            
            if (p.progress >= 1f)
            {
                p.active = false;
                p.go.SetActive(false);
                continue;
            }

            Vector3 m1 = Vector3.Lerp(p.startPos, p.controlPoint, p.progress);
            Vector3 m2 = Vector3.Lerp(p.controlPoint, p.target.position, p.progress);
            p.go.transform.position = Vector3.Lerp(m1, m2, p.progress);
        }
    }
}
