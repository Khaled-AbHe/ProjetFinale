using UnityEngine;

/// <summary>
/// Simple enemy that patrols between two points.
/// Optionally chases the player when they get close.
/// Requires: Rigidbody2D, Collider2D on the enemy GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Points")]
    public Transform pointA;               // Left patrol boundary (empty GameObjects)
    public Transform pointB;               // Right patrol boundary

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float waitTime = 0.5f;          // Pause at each end point

    [Header("Player Detection")]
    public float detectionRange = 4f;
    public LayerMask playerLayer;          // Set to your Player layer in Inspector
    public bool canChase = true;

    // Private state
    private Rigidbody2D rb;
    private Transform currentTarget;
    private bool isWaiting;
    private float waitTimer;
    private Transform player;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        currentTarget = pointB;            // Start by walking toward B

        // Find player by tag so we don't need a direct reference
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        // Check for nearby player
        bool playerInRange = player != null &&
                             Vector2.Distance(transform.position, player.position) <= detectionRange;

        if (canChase && playerInRange)
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
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) isWaiting = false;
            return;
        }

        // Move toward current patrol point
        MoveToward(currentTarget.position, patrolSpeed);

        // Switch target when close enough
        if (Vector2.Distance(transform.position, currentTarget.position) < 0.2f)
        {
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    void ChasePlayer()
    {
        MoveToward(player.position, chaseSpeed);
    }

    void MoveToward(Vector3 destination, float speed)
    {
        float direction = Mathf.Sign(destination.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

        // Flip sprite
        transform.localScale = new Vector3(direction, 1f, 1f);
    }

    // Visualise detection range and patrol path in Scene view
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
