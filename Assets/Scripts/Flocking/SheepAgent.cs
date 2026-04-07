using UnityEngine;
using System.Collections.Generic;

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

    [Header("Obstacle Avoidance")]
    [SerializeField]
    [Tooltip("Layers this sheep should treat as obstacles")]
    private LayerMask obstacleLayers;

    private Vector2 currentVelocity;
    private readonly Collider2D[] nearbyObstacles = new Collider2D[16];
    private readonly List<SheepAgent> neighbors = new();
    private Rigidbody2D rb;
    private Collider2D sheepCollider;
    private FlockManager flockManager;

    public Vector2 CurrentVelocity => currentVelocity;
    public IReadOnlyList<SheepAgent> Neighbors => neighbors;
    public int NeighborCount => neighbors.Count;

    // lets a spawner choose the sheep's initial heading before play starts
    public void SetStartingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        startingDirection = direction.normalized;
    }

    // lets the sheep look up the rest of the spawned flock
    public void SetFlockManager(FlockManager manager)
    {
        flockManager = manager;
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

        currentVelocity = direction * settings.MoveSpeed;
    }

    // steers and moves the sheep during the physics step
    private void FixedUpdate()
    {
        UpdateNeighbors();
        ApplySeparation();
        bool isAvoidingObstacle = ApplyObstacleAvoidance();
        ClampSpeed();
        if (!isAvoidingObstacle)
        {
            MaintainMoveSpeed();
        }
        rb.linearVelocity = currentVelocity;
    }

    // pushes the sheep away from nearby neighbors that are too close
    private void ApplySeparation()
    {
        if (settings == null || neighbors.Count == 0)
        {
            return;
        }

        float separationRadiusSqr = settings.SeparationRadius * settings.SeparationRadius;
        Vector2 separationDirection = Vector2.zero;
        Vector2 position = rb.position;

        for (int i = 0; i < neighbors.Count; i++)
        {
            SheepAgent neighbor = neighbors[i];

            if (neighbor == null)
            {
                continue;
            }

            Vector2 awayFromNeighbor = position - neighbor.rb.position;
            float distanceSqr = awayFromNeighbor.sqrMagnitude;

            if (distanceSqr <= 0.0001f || distanceSqr > separationRadiusSqr)
            {
                continue;
            }

            // gives a stronger push when another sheep is very close
            separationDirection += awayFromNeighbor.normalized / distanceSqr;
        }

        if (separationDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 desiredVelocity = separationDirection.normalized * settings.MoveSpeed;
        Vector2 steeringForce = (desiredVelocity - currentVelocity) * settings.SeparationWeight;
        steeringForce = Vector2.ClampMagnitude(steeringForce, settings.MaxSteeringForce * Time.fixedDeltaTime);

        currentVelocity += steeringForce;
    }

    // rebuilds the nearby sheep list using the shared neighbor radius
    private void UpdateNeighbors()
    {
        neighbors.Clear();

        if (flockManager == null)
        {
            return;
        }

        if (settings == null)
        {
            return;
        }

        float neighborRadiusSqr = settings.NeighborRadius * settings.NeighborRadius;
        Vector2 position = rb.position;
        IReadOnlyList<SheepAgent> flockSheep = flockManager.Sheep;

        for (int i = 0; i < flockSheep.Count; i++)
        {
            SheepAgent otherSheep = flockSheep[i];

            if (otherSheep == null || otherSheep == this)
            {
                continue;
            }

            Vector2 offset = otherSheep.rb.position - position;

            if (offset.sqrMagnitude > neighborRadiusSqr)
            {
                continue;
            }

            neighbors.Add(otherSheep);
        }
    }

    // steers the sheep away from nearby fence colliders
    private bool ApplyObstacleAvoidance()
    {
        Vector2 position = rb.position;
        int obstacleCount = Physics2D.OverlapCircle(position, settings.ObstacleCheckRadius, new ContactFilter2D
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
            float weight = 1f - Mathf.Clamp01(distance / settings.ObstacleCheckRadius);
            avoidanceDirection += awayFromObstacle.normalized * weight;
        }

        if (avoidanceDirection.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        // turns the sheep away from the nearest fence pressure
        Vector2 desiredVelocity = avoidanceDirection.normalized * settings.MoveSpeed;
        Vector2 steeringForce = desiredVelocity - currentVelocity;
        steeringForce = Vector2.ClampMagnitude(steeringForce, settings.MaxSteeringForce * Time.fixedDeltaTime);

        currentVelocity += steeringForce;
        return true;
    }

    // prevents runtime movement from exceeding the speed limit in settings
    private void ClampSpeed()
    {
        currentVelocity = Vector2.ClampMagnitude(currentVelocity, settings.MoveSpeed);
    }

    // keeps the sheep moving at its normal cruising speed after steering changes its velocity
    private void MaintainMoveSpeed()
    {
        if (currentVelocity.sqrMagnitude <= 0.0001f)
        {
            currentVelocity = GetStartingDirection() * settings.MoveSpeed;
            return;
        }

        currentVelocity = currentVelocity.normalized * settings.MoveSpeed;
    }

    // uses the chosen start direction when valid otherwise falls back to the right
    private Vector2 GetStartingDirection()
    {
        return startingDirection.sqrMagnitude > 0f
            ? startingDirection.normalized
            : Vector2.right;
    }

    // draws the neighbor radius when the debug panel toggle is enabled
    private void OnDrawGizmos()
    {
        if (!BoidDebugPanel.ShowNeighborRadius || settings == null)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.85f, 1f, 1f);
        Gizmos.DrawWireSphere(transform.position, settings.NeighborRadius);
    }
}
