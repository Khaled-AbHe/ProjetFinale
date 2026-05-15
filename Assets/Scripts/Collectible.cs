using UnityEngine;

/// <summary>
/// Collectible pickup (coins, stars, gems, etc.).
/// Attach to any pickup GameObject with a trigger Collider2D.
/// The player GameObject must be tagged "Player".
/// </summary>
public class Collectible : MonoBehaviour
{
    [Header("Settings")]
    public int scoreValue = 10;            // Points awarded on collection
    public bool destroyOnCollect = true;   // Remove the object after pickup

    [Header("Effects")]
    public GameObject collectEffect;       // Optional particle effect prefab
    public AudioClip collectSound;         // Optional sound effect

    [Header("Bob Animation (optional)")]
    public bool bobUpAndDown = true;
    public float bobSpeed = 2f;
    public float bobHeight = 0.15f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (bobUpAndDown)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    private void Collect()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }
        else
        {
            Debug.LogWarning($"Collectible on '{gameObject.name}': GameManager.Instance is null — " +
                             "score was not recorded.");
        }

        if (collectEffect != null)
            Instantiate(collectEffect, transform.position, Quaternion.identity);

        // PlayClipAtPoint works even after this object is destroyed
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false); // Hide instead of destroy (pool-friendly)
    }
}
