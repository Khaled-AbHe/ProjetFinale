using System.Collections;
using UnityEngine;

/// <summary>
/// Frog tongue grapple hook — physics, input, and visuals in one script.
/// Attach this to your frog player GameObject.
///
/// Joint behaviour:
///   - While GROUNDED with tongue attached: visual only, no joint. The player
///     can walk freely. Joint is created if they reach maxLength on the ground.
///   - When AIRBORNE with tongue attached: joint is created at the current
///     distance, pendulum physics take over.
///   - On LANDING: joint is destroyed. Visual stays until the player releases.
///
/// SETUP CHECKLIST:
///   1. Add this script to your frog GameObject.
///   2. Add a LineRenderer component to your frog GameObject and assign it
///      to the lineRenderer field in the Inspector. Set your tongue color (e.g. pink).
///      Recommended: Width ~0.05, round end caps.
///   3. Create an empty child GameObject, add a SpriteRenderer with a circle sprite
///      (pink), name it "TongueTip", and assign its Transform to the tongueTip field.
///   4. Create a Layer called "Grappleable" and assign it to your Tilemap.
///   5. Assign the "Grappleable" layer to the grappleLayer field in the Inspector.
///   6. Your Tilemap must have: TilemapCollider2D (Used By Composite = true)
///      and CompositeCollider2D. The CompositeCollider2D is what the raycast hits.
///   7. (Optional) Assign missEffect, missSound, and hitSound for juice feedback.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TongueHook : MonoBehaviour
{
    // ── Grapple Settings ──────────────────────────────────────────────────────

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

    // ── Visual Settings ───────────────────────────────────────────────────────

    [Header("Tongue Visual")]
    [Tooltip("LineRenderer component on this GameObject")]
    public LineRenderer lineRenderer;

    [Tooltip("Transform of the small ball at the tongue tip. Child GameObject with a SpriteRenderer.")]
    public Transform tongueTip;

    [Tooltip("Offset from the frog's center where the tongue shoots from (local space). " +
             "Match this to your frog sprite's mouth position.")]
    public Vector2 tongueOriginOffset = Vector2.zero;

    // ── Feedback ──────────────────────────────────────────────────────────────

    [Header("Miss Feedback")]
    [Tooltip("Particle/prefab spawned at max range when the tongue hits nothing")]
    public GameObject missEffect;
    public AudioClip  missSound;

    [Header("Hit Feedback")]
    [Tooltip("Sound played at the anchor point when the tongue successfully attaches")]
    public AudioClip hitSound;

    // ── Private State ─────────────────────────────────────────────────────────

    private Rigidbody2D     rb;
    private DistanceJoint2D joint;

    private bool isHooking  = false; // Tongue is attached (visual + possibly joint)
    private bool isTravelling = false; // Tongue is animating toward anchor
    private bool isGrounded   = false; // Updated by PlayerController via NotifyGrounded
    private bool wasGrounded  = false; // Previous frame's grounded state for transition detection
    private bool isTongueVisible = false; // Tracks visual state to avoid redundant SetActive calls

    private Vector2   anchorPoint;
    private Vector2   travelTip;
    private Coroutine travelCoroutine;

    // The cooldown guards against the toggle case where fire and release are
    // the same button — without it a single press could fire and immediately
    // release within the same frame depending on script execution order.
    private const float InputCooldown = 0.1f;
    private float lastFireTime = -999f;

    // Convenience property — true whenever the tongue is doing anything visible
    private bool IsTongueActive => isHooking || isTravelling;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.enabled       = false;

        HideTip();
    }

    void Update()
    {
        HandleFireInput();
    }

    void FixedUpdate()
    {
        HandleGroundedTransitions();
        HandlePullExtend();
        HandleGroundedMaxLength();
    }

    void LateUpdate()
    {
        UpdateVisuals();
    }

    // ── Grounded State ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by PlayerController every FixedUpdate with the current grounded state.
    /// Storing it here keeps all grounded logic centralised in GrappleHook.
    /// </summary>
    public void NotifyGrounded(bool grounded)
    {
        isGrounded = grounded;
    }

    /// <summary>
    /// Detects ground transitions and creates or destroys the joint accordingly.
    /// Runs in FixedUpdate so it stays in sync with physics.
    /// </summary>
    void HandleGroundedTransitions()
    {
        if (!isHooking)
        {
            wasGrounded = isGrounded;
            return;
        }

        if (wasGrounded && !isGrounded)
            CreateJoint();       // Just left the ground — start swinging
        else if (!wasGrounded && isGrounded)
            DestroyJoint();      // Just landed — hand control back to PlayerController

        wasGrounded = isGrounded;
    }

    /// <summary>
    /// While grounded with no joint, creates one once the player reaches maxLength
    /// so the tongue goes taut and prevents them walking further.
    /// </summary>
    void HandleGroundedMaxLength()
    {
        if (!isHooking || joint != null || !isGrounded) return;

        if (Vector2.Distance(transform.position, anchorPoint) >= maxLength)
            CreateJoint();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    void HandleFireInput()
    {
        bool holdActive  = Input.GetMouseButton(1);      // RMB held
        bool holdStarted = Input.GetMouseButtonDown(1);  // RMB just pressed
        bool holdEnded   = Input.GetMouseButtonUp(1);    // RMB just released

        // Fire on initial press (not every frame of the hold)
        if (holdStarted && !IsTongueActive)
        {
            if (Time.time - lastFireTime >= InputCooldown)
            {
                lastFireTime = Time.time;
                TryFireGrapple();
            }
        }

        // Release when the button is lifted, not on re-press
        if (holdEnded && IsTongueActive)
            ReleaseGrapple();
    }

    /// <summary>
    /// Adjusts joint.distance in FixedUpdate so pull/extend stays in sync
    /// with the physics step and behaves consistently at any framerate.
    /// </summary>
    void HandlePullExtend()
    {
        if (!isHooking || joint == null) return;

        if (Input.GetKey(KeyCode.W))
            joint.distance = Mathf.Max(minLength, joint.distance - pullSpeed   * Time.fixedDeltaTime);
        else if (Input.GetKey(KeyCode.S))
            joint.distance = Mathf.Min(maxLength, joint.distance + extendSpeed * Time.fixedDeltaTime);
    }

    // ── Grapple Logic ─────────────────────────────────────────────────────────

    void TryFireGrapple()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction     = (mouseWorldPos - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxLength, grappleLayer);

        if (hit.collider == null)
        {
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

    IEnumerator TongueTravelRoutine(Vector2 target)
    {
        isTravelling = true;
        travelTip    = (Vector2)transform.position + tongueOriginOffset;

        SetTongueVisible(true);

        while (Vector2.Distance(travelTip, target) > 0.05f)
        {
            travelTip = Vector2.MoveTowards(travelTip, target, tongueSpeed * Time.deltaTime);
            yield return null;
        }

        isTravelling = false;
        AttachTongue(target);

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, target);
    }

    void AttachTongue(Vector2 anchor)
    {
        anchorPoint = anchor;
        isHooking = true;

        // Create the joint immediately if already airborne
        if (!isGrounded)
            CreateJoint();
    }

    void CreateJoint()
    {
        if (joint != null) return;

        joint                 = gameObject.AddComponent<DistanceJoint2D>();
        joint.connectedAnchor = anchorPoint;
        joint.distance        = Vector2.Distance(transform.position, anchorPoint);
        joint.maxDistanceOnly = true;
        joint.enableCollision = true;
    }

    void DestroyJoint()
    {
        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }
    }

    public void ReleaseGrapple()
    {
        if (travelCoroutine != null)
        {
            StopCoroutine(travelCoroutine);
            travelCoroutine = null;
        }

        if (isHooking && joint != null && releaseBoostMultiplier > 0f)
        {
            Vector2 vel = rb.linearVelocity;
            if (vel.sqrMagnitude > 0.01f)
                rb.AddForce(vel.normalized * vel.magnitude * releaseBoostMultiplier,
                            ForceMode2D.Impulse);
        }

        DestroyJoint();

        isHooking  = false;
        isTravelling = false;

        SetTongueVisible(false);
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    void UpdateVisuals()
    {
        if (!IsTongueActive)
        {
            // Only call SetActive if the tongue is currently visible —
            // avoids redundant SetActive(false) calls every frame when idle.
            if (isTongueVisible)
                SetTongueVisible(false);

            return;
        }

        Vector2 origin = (Vector2)transform.position + tongueOriginOffset;
        Vector2 target = isTravelling ? travelTip : anchorPoint;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, target);

        if (tongueTip != null)
            tongueTip.position = target;
    }

    /// <summary>
    /// Single point of control for showing and hiding the tongue visual.
    /// Prevents scattered enable/disable calls and redundant SetActive calls.
    /// </summary>
    void SetTongueVisible(bool visible)
    {
        if (isTongueVisible == visible) return;

        isTongueVisible      = visible;
        lineRenderer.enabled = visible;

        if (tongueTip != null)
            tongueTip.gameObject.SetActive(visible);
    }

    void HideTip()
    {
        if (tongueTip != null)
            tongueTip.gameObject.SetActive(false);
    }

    // ── Public Accessors ──────────────────────────────────────────────────────

    public bool    IsHooking  => isHooking;
    public bool    IsTravelling => isTravelling;
    public Vector2 AnchorPoint  => anchorPoint;
}