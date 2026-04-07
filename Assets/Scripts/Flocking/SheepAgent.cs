using UnityEngine;

// handles basic movement for one sheep before any flocking behavior is added
[RequireComponent(typeof(Rigidbody2D))]
public class SheepAgent : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [Header("References")]
    [SerializeField]
    [Tooltip("Shared movement settings used by this sheep")]
    private BoidSettings settings;

    // -------------------------------------------------------------------------------------------------------------

    [Header("Startup")]
    [SerializeField]
    [Tooltip("Direction this sheep starts moving in if no velocity is set")]
    private Vector2 startingDirection = Vector2.right;

    // -------------------------------------------------------------------------------------------------------------

    [Header("Movement")]
    [SerializeField, Min(0f)]
    [Tooltip("Normal speed this sheep tries to maintain while moving")]
    private float moveSpeed = 4f;

    // -------------------------------------------------------------------------------------------------------------

    [Header("Obstacle Avoidance")]
    [SerializeField]
    [Tooltip("Layers this sheep should treat as obstacles")]
    private LayerMask obstacleLayers;

    [SerializeField, Min(0f)]
    [Tooltip("How far this sheep checks for nearby obstacles")]
    private float obstacleCheckRadius = 1.5f;

    private Vector2 currentVelocity;
    private readonly Collider2D[] nearbyObstacles = new Collider2D[16];
    private Rigidbody2D rb;
    private Collider2D sheepCollider;

    public Vector2 CurrentVelocity => currentVelocity;

    // lets a spawner choose the sheep's initial heading before play starts
    public void SetStartingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        startingDirection = direction.normalized;
    }

    // grabs the rigidbody used to move the sheep through the physics system
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sheepCollider = GetComponent<Collider2D>();
    }

    // gives the sheep a valid starting velocity when play mode begins
    private void Start()
    {
        if (settings == null)
        {
            Debug.LogError("SheepAgent needs a BoidSettings asset assigned", this);
            enabled = false;
            return;
        }

        if (sheepCollider == null)
        {
            Debug.LogError("SheepAgent needs a Collider2D so fences can block movement", this);
            enabled = false;
            return;
        }

        // uses the chosen start direction when valid otherwise falls back to the right
        Vector2 direction = startingDirection.sqrMagnitude > 0f
            ? startingDirection.normalized
            : Vector2.right;

        currentVelocity = direction * moveSpeed;
    }

    // steers and moves the sheep during the physics step
    private void FixedUpdate()
    {
        bool isAvoidingObstacle = ApplyObstacleAvoidance();
        ClampSpeed();
        if (!isAvoidingObstacle)
        {
            MaintainMoveSpeed();
        }
        rb.linearVelocity = currentVelocity;
    }

    // steers the sheep away from nearby fence colliders
    private bool ApplyObstacleAvoidance()
    {
        Vector2 position = rb.position;
        int obstacleCount = Physics2D.OverlapCircle(position, obstacleCheckRadius, new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = obstacleLayers,
            useTriggers = false
        }, nearbyObstacles);

        if (obstacleCount == 0)
        {
            return false;
        }
        
        Vector2 avoidanceDirection = Vector2.zero;

        for (int i = 0; i < obstacleCount; i++)
        {
            Collider2D obstacle = nearbyObstacles[i];

            if (obstacle == null)
            {
                continue;
            }

            Vector2 closestPoint = obstacle.ClosestPoint(position);
            Vector2 awayFromObstacle = position - closestPoint;

            // ignores colliders that do not give a useful direction away from the obstacle
            if (awayFromObstacle.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            float distance = awayFromObstacle.magnitude;
            float weight = 1f - Mathf.Clamp01(distance / obstacleCheckRadius);
            avoidanceDirection += awayFromObstacle.normalized * weight;
        }

        if (avoidanceDirection.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        // turns the sheep away from the nearest fence pressure
        Vector2 desiredVelocity = avoidanceDirection.normalized * moveSpeed;
        Vector2 steeringForce = desiredVelocity - currentVelocity;
        steeringForce = Vector2.ClampMagnitude(steeringForce, settings.MaxSteeringForce * Time.fixedDeltaTime);

        currentVelocity += steeringForce;
        return true;
    }

    // prevents runtime movement from exceeding the speed limit in settings
    private void ClampSpeed()
    {
        currentVelocity = Vector2.ClampMagnitude(currentVelocity, settings.MaxSpeed);
    }

    // keeps the sheep moving at its normal cruising speed after steering changes its velocity
    private void MaintainMoveSpeed()
    {
        if (currentVelocity.sqrMagnitude <= 0.0001f)
        {
            currentVelocity = GetStartingDirection() * moveSpeed;
            return;
        }

        currentVelocity = currentVelocity.normalized * moveSpeed;
    }

    // uses the chosen start direction when valid otherwise falls back to the right
    private Vector2 GetStartingDirection()
    {
        return startingDirection.sqrMagnitude > 0f
            ? startingDirection.normalized
            : Vector2.right;
    }
}
