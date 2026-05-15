using UnityEngine;

/// <summary>
/// Draws the frog's tongue as a straight line from the frog to the anchor point.
/// Supports a travel animation where the tip moves toward the anchor before attaching.
/// Attach this script to your frog GameObject alongside GrappleHook.cs.
///
/// SETUP CHECKLIST:
///   1. Add a LineRenderer component to your frog GameObject.
///   2. Assign that LineRenderer to the lineRenderer field in the Inspector.
///   3. Set your tongue material/color (e.g. pink) on the LineRenderer.
///      Recommended: Width ~0.05, round end caps.
///   4. Assign this component to GrappleHook's ropeRenderer field.
/// </summary>
public class GrappleRope : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The LineRenderer component on this GameObject")]
    public LineRenderer lineRenderer;

    [Header("Tongue Origin")]
    [Tooltip("Offset from the frog's center where the tongue shoots from (local space). " +
             "Match this to your frog sprite's mouth position.")]
    public Vector2 tongueOriginOffset = Vector2.zero;

    // ── State ────────────────────────────────────────────────────────────────
    private bool    active      = false;
    private Vector2 anchorPoint;

    private bool    isTravelling = false;
    private Vector2 travelTip;

    public Vector2 TongueOriginOffset => tongueOriginOffset;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.enabled       = false;
    }

    void LateUpdate()
    {
        if (!active && !isTravelling) return;

        Vector2 origin = (Vector2)transform.position + tongueOriginOffset;
        Vector2 target = isTravelling ? travelTip : anchorPoint;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, target);
    }

    // ── Public API (called by GrappleHook) ───────────────────────────────────

    /// <summary>
    /// Called at the start of the tongue travel animation.
    /// </summary>
    public void BeginTravel(Vector2 target)
    {
        travelTip            = (Vector2)transform.position + tongueOriginOffset;
        isTravelling         = true;
        active               = false;
        lineRenderer.enabled = true;
    }

    /// <summary>
    /// Called each frame by GrappleHook's travel coroutine with the current tip position.
    /// </summary>
    public void UpdateTravelTip(Vector2 tip)
    {
        travelTip = tip;
    }

    /// <summary>
    /// Called by GrappleHook once the tongue is fully attached (enabled = true)
    /// or released (enabled = false).
    /// </summary>
    public void SetGrapple(Vector2 anchor, bool enabled)
    {
        anchorPoint          = anchor;
        active               = enabled;
        isTravelling         = false;
        lineRenderer.enabled = enabled;
    }
}