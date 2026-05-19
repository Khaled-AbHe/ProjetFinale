using System.Collections;
using UnityEngine;

// soruce: https://www.youtube.com/watch?v=5R0FgRNvBcM
public class EnemyEntity : MonoBehaviour
{
    [Header("Patrol Points")]
    public Transform pointA;
    public Transform pointB;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed  = 3.5f;
    public float waitTime    = 0.5f;

    [Header("Player Detection")]
    public float detectionRange = 4f;
    public LayerMask playerLayer;

    [Header("Health")]
    public float maxHealth = 30f;
    public float contactDamage = 10f;

    [Header("Hit Flash")]
    public float flashDuration = 0.15f;

    [Header("Settings")]
    public int scoreValue = 1;

    private Rigidbody2D rb;
    private Vector3 originalScale;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float currentHealth;

    // Patrol
    private Transform patrolTarget;
    private bool isWaiting;
    private float waitTimer;
    private float distance;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        originalScale = transform.localScale;
        currentHealth = maxHealth;
        patrolTarget = pointB;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj.transform;
    }

    void Update()
    {
        distance = Vector2.Distance(transform.position, player.position);
    }   

    void FixedUpdate()
    {
        if (distance <= detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        if (pointA == null || pointB == null) return;

        if (isWaiting)
        {
            // stop moving
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            // timer
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0f) isWaiting = false;
            animator.SetBool("IsMoving", false);
            return;
        }

        bool enemyIsCloseToPoint = Mathf.Abs(patrolTarget.position.x - transform.position.x) <= 0.05f;

        if (enemyIsCloseToPoint)
        {
            // stop moving
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetBool("IsMoving", false);
            // switches point
            patrolTarget = (patrolTarget == pointA) ? pointB : pointA;
            // waiting activated
            isWaiting = true;
            waitTimer = waitTime;
            return;
        }

        // keep moving
        MoveToward(patrolTarget.position, patrolSpeed);
    }

    void ChasePlayer()
    {
        MoveToward(player.position, chaseSpeed);
    }

    void MoveToward(Vector3 destination, float speed)
    {
        float direction = Mathf.Sign(destination.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

        transform.localScale = new Vector3(
            Mathf.Abs(originalScale.x) * direction,
            originalScale.y,
            originalScale.z
        );

        animator.SetBool("IsMoving", true);
    }

    // Damage Player
    void OnCollisionEnter2D(Collision2D collision)
    {
        HealthSystem playerHealth = collision.gameObject.GetComponent<HealthSystem>();
        playerHealth.TakeDamage(contactDamage);
    }

    // Enemy HP
    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        // makes sure it doesnt go below 0
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (currentHealth <= 0)
        {
            GameManager.Instance.AddScore(scoreValue);
            Destroy(gameObject);
            return;
        }

        StartCoroutine(FlashRed());
    }

    IEnumerator FlashRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = Color.white;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawSphere(pointA.position, 0.15f);
            Gizmos.DrawSphere(pointB.position, 0.15f);
        }
    }
}