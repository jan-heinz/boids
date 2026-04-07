using UnityEngine;

// shared boid tuning values used by the flock systems
[CreateAssetMenu(fileName = "BoidSettings", menuName = "Boids/Boid Settings")]
public class BoidSettings : ScriptableObject
{
    // -------------------------------------------------------------------------------------------------------------

    [Header("Movement")]
    [SerializeField, Min(0f)]
    [Tooltip("Default movement speed each sheep tries to maintain")]
    private float moveSpeed = 4f;

    [SerializeField, Min(0f)]
    [Tooltip("Maximum steering force applied in a single update")]
    private float maxSteeringForce = 3f;

    // -------------------------------------------------------------------------------------------------------------

    [Header("Neighbor Detection")]
    [SerializeField, Min(0f)]
    [Tooltip("How far a sheep can look to find nearby flockmates")]
    private float neighborRadius = 2.5f;

    [SerializeField, Min(0f)]
    [Tooltip("How close another sheep can get before separation pushes away")]
    private float separationRadius = 1f;

    [SerializeField, Min(0f)]
    [Tooltip("How far a sheep checks for nearby obstacles")]
    private float obstacleCheckRadius = 1.5f;

    // -------------------------------------------------------------------------------------------------------------

    [Header("Behavior Weights")]
    [SerializeField, Min(0f)]
    [Tooltip("How strongly sheep steer away from nearby flockmates")]
    private float separationWeight = 1.5f;

    [SerializeField, Min(0f)]
    [Tooltip("How strongly sheep match the heading of nearby flockmates")]
    private float alignmentWeight = 1f;

    [SerializeField, Min(0f)]
    [Tooltip("How strongly sheep steer toward the local group center")]
    private float cohesionWeight = 1f;

    [SerializeField, Min(0f)]
    [Tooltip("How strongly sheep are pushed back toward the sandbox when near an edge")]
    private float boundsWeight = 2f;

    public float MoveSpeed
    {
        get => moveSpeed;
        set
        {
            moveSpeed = Mathf.Max(0f, value);
            ClampValues();
        }
    }

    public float MaxSteeringForce
    {
        get => maxSteeringForce;
        set => maxSteeringForce = Mathf.Max(0f, value);
    }

    public float NeighborRadius
    {
        get => neighborRadius;
        set
        {
            neighborRadius = Mathf.Max(0f, value);
            ClampValues();
        }
    }

    public float SeparationRadius
    {
        get => separationRadius;
        set
        {
            separationRadius = Mathf.Max(0f, value);
            ClampValues();
        }
    }

    public float ObstacleCheckRadius
    {
        get => obstacleCheckRadius;
        set => obstacleCheckRadius = Mathf.Max(0f, value);
    }

    public float SeparationWeight
    {
        get => separationWeight;
        set => separationWeight = Mathf.Max(0f, value);
    }

    public float AlignmentWeight
    {
        get => alignmentWeight;
        set => alignmentWeight = Mathf.Max(0f, value);
    }

    public float CohesionWeight
    {
        get => cohesionWeight;
        set => cohesionWeight = Mathf.Max(0f, value);
    }

    public float BoundsWeight
    {
        get => boundsWeight;
        set => boundsWeight = Mathf.Max(0f, value);
    }

    // keeps related values in a valid range
    private void ClampValues()
    {
        separationRadius = Mathf.Min(separationRadius, neighborRadius);
    }

    // reapplies the same value guards after inspector edits
    private void OnValidate()
    {
        ClampValues();
    }
}
