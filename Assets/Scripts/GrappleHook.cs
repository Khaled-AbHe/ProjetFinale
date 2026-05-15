using System.Collections;
using UnityEngine;

/// <summary>
/// Frog tongue grapple hook controller.
/// Attach this to your frog player GameObject.
///
/// SETUP CHECKLIST:
///   1. Add this script and GrappleRope.cs to your frog GameObject.
///   2. Create a Layer called "Grappleable" and assign it to your Tilemap GameObject.
///   3. Assign the "Grappleable" layer to the grappleLayer field in the Inspector.
///   4. Assign the GrappleRope component to the ropeRenderer field in the Inspector.
///   5. Your Tilemap must have: TilemapCollider2D (Used By Composite = true)
///      and CompositeCollider2D. The CompositeCollider2D is what the raycast hits.
///   6. (Optional) Assign missEffect, missSound, and hitSound for juice feedback.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class GrappleHook : MonoBehaviour
{
    [Header("Grapple Settings")]
    [Tooltip("Maximum reach of the tongue")]
    public float maxLength = 10f;

    [Tooltip("Minimum rope length when pulling")]
    public float minLength = 1f;

    [Tooltip("How fast pulling shortens the tongue")]
    public float pullSpeed = 3f;

    [Tooltip("How fast extending lengthens the tongue")]
    public float extendSpeed = 3f;

    [Header("Tongue Travel")]
    [Tooltip("Speed (units/sec) at which the tongue tip travels to the anchor before attaching")]
    public float tongueSpeed = 20f;

    [Header("Release Boost")]
    [Tooltip("Multiplier applied to velocity on release for a satisfying momentum kick. 0 = no boost.")]
    public float releaseBoostMultiplier = 0.3f;

    [Header("Layer")]
    [Tooltip("Set this to the layer your Tilemap Grappleable surfaces are on")]
    public LayerMask grappleLayer;

    [Header("References")]
    public GrappleRope ropeRenderer;

    [Header("Miss Feedback")]
    [Tooltip("Particle/prefab spawned at max range when the tongue hits nothing")]
    public GameObject missEffect;
    public AudioClip missSound;

    [Header("Hit Feedback")]
    [Tooltip("Sound played at the anchor point when the tongue successfully attaches")]
    public AudioClip hitSound;

    // ── State ────────────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private DistanceJoint2D joint;

    private bool isGrappling  = false;
    private bool isTravelling = false;

    private Vector2 anchorPoint;
    private Coroutine travelCoroutine;

    private const float InputCooldown = 0.1f;
    private float lastFireTime = -999f;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleFireInput();

        if (isGrappling)
            HandlePullExtend();
    }

    // ── Fire / Release ───────────────────────────────────────────────────────

    void HandleFireInput()
    {
        bool firePressed = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetMouseButtonDown(0);

        if (!firePressed) return;
        if (Time.time - lastFireTime < InputCooldown) return;

        lastFireTime = Time.time;

        if (isGrappling || isTravelling)
            ReleaseGrapple();
        else
            TryFireGrapple();
    }

    void TryFireGrapple()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction     = (mouseWorldPos - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxLength, grappleLayer);

        if (hit.collider == null)
        {
            // ── Miss feedback ────────────────────────────────────────────────
            Vector2 missPos = (Vector2)transform.position + direction * maxLength;

            if (missEffect != null)
                Instantiate(missEffect, missPos, Quaternion.identity);

            if (missSound != null)
                AudioSource.PlayClipAtPoint(missSound, missPos);

            return;
        }

        anchorPoint     = hit.point;
        travelCoroutine = StartCoroutine(TongueTravelRoutine(anchorPoint));
    }

    /// <summary>
    /// Animates the tongue tip from the frog's mouth to the anchor point before
    /// creating the physics joint.
    /// </summary>
    IEnumerator TongueTravelRoutine(Vector2 target)
    {
        isTravelling = true;
        ropeRenderer.BeginTravel(target);

        Vector2 tip = (Vector2)transform.position + ropeRenderer.TongueOriginOffset;

        while (Vector2.Distance(tip, target) > 0.05f)
        {
            tip = Vector2.MoveTowards(tip, target, tongueSpeed * Time.deltaTime);
            ropeRenderer.UpdateTravelTip(tip);
            yield return null;
        }

        isTravelling = false;
        AttachGrapple(target);

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, target);
    }

    void AttachGrapple(Vector2 anchor)
    {
        joint                  = gameObject.AddComponent<DistanceJoint2D>();
        joint.connectedAnchor  = anchor;
        joint.distance         = Vector2.Distance(transform.position, anchor);
        joint.maxDistanceOnly  = true;
        joint.enableCollision  = true;

        isGrappling = true;
        ropeRenderer.SetGrapple(anchor, true);
    }

    public void ReleaseGrapple()
    {
        if (travelCoroutine != null)
        {
            StopCoroutine(travelCoroutine);
            travelCoroutine = null;
        }

        // ── Momentum boost ───────────────────────────────────────────────────
        if (isGrappling && releaseBoostMultiplier > 0f)
        {
            Vector2 vel = rb.linearVelocity;
            if (vel.sqrMagnitude > 0.01f)
                rb.AddForce(vel.normalized * vel.magnitude * releaseBoostMultiplier,
                            ForceMode2D.Impulse);
        }

        if (joint != null)
            Destroy(joint);

        isGrappling  = false;
        isTravelling = false;

        ropeRenderer.SetGrapple(Vector2.zero, false);
    }

    // ── Pull / Extend ────────────────────────────────────────────────────────

    void HandlePullExtend()
    {
        if (Input.GetKey(KeyCode.W))
            joint.distance = Mathf.Max(minLength,  joint.distance - pullSpeed   * Time.deltaTime);
        else if (Input.GetKey(KeyCode.S))
            joint.distance = Mathf.Min(maxLength,  joint.distance + extendSpeed * Time.deltaTime);
    }

    // ── Public Accessors ─────────────────────────────────────────────────────

    public bool    IsGrappling  => isGrappling;
    public bool    IsTravelling => isTravelling;
    public Vector2 AnchorPoint  => anchorPoint;
}