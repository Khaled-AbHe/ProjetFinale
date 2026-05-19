using System.Collections;
using UnityEngine;

public class SpitSplash : MonoBehaviour
{
    [Header("Droplet Count")]
    public int dropletCount = 8;

    [Header("Droplet Size")]
    public float minSize = 0.04f;
    public float maxSize = 0.12f;

    [Header("Color")]
    public Color dropletColor = new Color(0.6f, 0.9f, 0.4f, 1f);

    void Start()
    {
        for (int i = 0; i < dropletCount; i++)
        {
            SpawnDroplet();
        }

        Destroy(gameObject, 0.5f + 0.1f);
    }

    void SpawnDroplet()
    {
        GameObject droplet = new GameObject("Droplet");
        droplet.transform.position = transform.position;

        SpriteRenderer sr = droplet.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite();
        sr.color  = dropletColor;

        // Random size
        float size = Random.Range(minSize, maxSize);
        droplet.transform.localScale = new Vector3(size, size, 1f);

        // Passthrough
        Rigidbody2D rb      = droplet.AddComponent<Rigidbody2D>();
        rb.gravityScale     = 1.5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Random directions
        float   angle     = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float   speed     = Random.Range(2f, 6f);
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        rb.linearVelocity = direction * speed;

        StartCoroutine(FadeAndDestroy(droplet, sr));
    }

    IEnumerator FadeAndDestroy(GameObject droplet, SpriteRenderer sr)
    {
        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < 0.5f)
        {
            if (droplet == null) yield break;

            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        if (droplet != null)
            Destroy(droplet);
    }

    private static Sprite cachedCircleSprite;
    static Sprite MakeCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        const int size = 32;
        const int center = size / 2;
        const int radius = size / 2;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - (dist - (radius - 1f)));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedCircleSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );

        return cachedCircleSprite;
    }
}