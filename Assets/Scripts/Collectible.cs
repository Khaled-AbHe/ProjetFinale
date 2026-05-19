using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Settings")]
    public int scoreValue = 5;

    [Header("Bob Animation")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.15f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) Collect();
    }

    private void Collect()
    {
        GameManager.Instance.AddScore(scoreValue);
        Destroy(gameObject);
    }
}
