using UnityEngine;
using System.Collections.Generic;

// acts as a pressure point that sheep try to avoid
public class Sheepdog : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [Header("Placement")]
    [SerializeField]
    [Tooltip("Whether the sheepdog starts placed when play begins")]
    private bool startsPlaced;

    [SerializeField]
    [Tooltip("How large the sheepdog's square blocking collider should be")]
    private float collisionSize = 1.5f;

    [SerializeField, Min(0f)]
    [Tooltip("How fast the sheepdog runs to a new clicked position")]
    private float moveSpeed = 12f;

    [SerializeField, Min(0f)]
    [Tooltip("How close the sheepdog needs to get before stopping")]
    private float stopDistance = 0.1f;

    [Header("Pressure")]
    [SerializeField, Min(0f)]
    [Tooltip("How far the sheepdog can influence nearby sheep")]
    private float influenceRadius = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("How close sheep can get before the pressure becomes strongest")]
    private float innerRadius = 2.5f;

    [SerializeField, Min(0f)]
    [Tooltip("How strongly the sheepdog pushes sheep away")]
    private float pressureStrength = 1.5f;

    [SerializeField, Min(1f)]
    [Tooltip("How much stronger the pressure becomes inside the inner radius")]
    private float innerPressureMultiplier = 3f;

    private static readonly List<Sheepdog> activeSheepdogs = new();
    private BoxCollider2D blockingCollider;
    private Rigidbody2D rb;
    private Renderer[] cachedRenderers;
    private bool isPlaced;
    private bool hasMoveTarget;
    private Vector2 moveTarget;

    public static IReadOnlyList<Sheepdog> ActiveSheepdogs => activeSheepdogs;
    public float CollisionSize => collisionSize;
    public float InfluenceRadius => influenceRadius;
    public float InnerRadius => innerRadius;
    public float PressureStrength => pressureStrength;
    public float InnerPressureMultiplier => innerPressureMultiplier;
    public bool IsPlaced => isPlaced;
    public bool IsMoving => hasMoveTarget;

    // caches renderers and applies the starting placed state
    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        blockingCollider = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        if (blockingCollider == null)
        {
            blockingCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        blockingCollider.isTrigger = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        UpdateBlockingCollider();
        SetPlaced(startsPlaced);
    }

    // moves the sheepdog toward the selected destination
    private void FixedUpdate()
    {
        if (!isPlaced || !hasMoveTarget)
        {
            return;
        }

        Vector2 position = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 toTarget = moveTarget - position;

        if (toTarget.magnitude <= stopDistance)
        {
            hasMoveTarget = false;

            if (rb != null)
            {
                rb.MovePosition(moveTarget);
            }
            else
            {
                transform.position = new Vector3(moveTarget.x, moveTarget.y, transform.position.z);
            }

            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(position, moveTarget, moveSpeed * Time.fixedDeltaTime);

        if (rb != null)
        {
            rb.MovePosition(nextPosition);
            return;
        }

        transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
    }

    // registers this sheepdog so sheep can read it without scene searches
    private void OnEnable()
    {
        if (!activeSheepdogs.Contains(this))
        {
            activeSheepdogs.Add(this);
        }
    }

    // removes this sheepdog when it is disabled or destroyed
    private void OnDisable()
    {
        activeSheepdogs.Remove(this);
    }

    // keeps the pressure radii in a valid range after inspector edits
    private void OnValidate()
    {
        collisionSize = Mathf.Max(0f, collisionSize);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        stopDistance = Mathf.Max(0f, stopDistance);
        influenceRadius = Mathf.Max(0f, influenceRadius);
        innerRadius = Mathf.Clamp(innerRadius, 0f, influenceRadius);
        pressureStrength = Mathf.Max(0f, pressureStrength);
        innerPressureMultiplier = Mathf.Max(1f, innerPressureMultiplier);

        if (blockingCollider != null)
        {
            UpdateBlockingCollider();
        }
    }

    // places the sheepdog at the requested world position
    public void Place(Vector2 worldPosition)
    {
        transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        moveTarget = worldPosition;
        hasMoveTarget = false;

        if (rb != null)
        {
            rb.position = worldPosition;
        }

        SetPlaced(true);
    }

    // tells the sheepdog to run toward a new spot
    public void MoveTo(Vector2 worldPosition)
    {
        if (!isPlaced)
        {
            Place(worldPosition);
            return;
        }

        moveTarget = worldPosition;
        hasMoveTarget = true;
    }

    // updates whether the sheepdog is active in the level
    public void SetPlaced(bool placed)
    {
        isPlaced = placed;

        if (blockingCollider != null)
        {
            blockingCollider.enabled = placed;
        }

        if (cachedRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null)
            {
                continue;
            }

            cachedRenderers[i].enabled = placed;
        }
    }

    // converts distance from the sheepdog into a pressure amount
    public float GetPressure(float distance)
    {
        if (distance <= innerRadius)
        {
            return pressureStrength * innerPressureMultiplier;
        }

        if (distance >= influenceRadius)
        {
            return 0f;
        }

        float falloff = 1f - Mathf.InverseLerp(innerRadius, influenceRadius, distance);
        return pressureStrength * falloff;
    }

    // keeps the blocking collider matched to the square sheepdog size
    private void UpdateBlockingCollider()
    {
        if (blockingCollider == null)
        {
            return;
        }

        blockingCollider.size = Vector2.one * collisionSize;
    }

    // draws the pressure ranges while tuning in the editor
    private void OnDrawGizmosSelected()
    {
        if (!isPlaced && Application.isPlaying)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.4f, 1f, 0.45f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * collisionSize);
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
        Gizmos.DrawSphere(transform.position, influenceRadius);
        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.35f);
        Gizmos.DrawSphere(transform.position, innerRadius);
    }
}
