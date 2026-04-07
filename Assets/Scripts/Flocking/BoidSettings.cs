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
    [Tooltip("Maximum speed a sheep is allowed to reach")]
    private float maxSpeed = 6f;

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

    public float MoveSpeed => moveSpeed;
    public float MaxSpeed => maxSpeed;
    public float MaxSteeringForce => maxSteeringForce;
    public float NeighborRadius => neighborRadius;
    public float SeparationRadius => separationRadius;
    public float SeparationWeight => separationWeight;
    public float AlignmentWeight => alignmentWeight;
    public float CohesionWeight => cohesionWeight;
    public float BoundsWeight => boundsWeight;

    private void OnValidate()
    {
        maxSpeed = Mathf.Max(maxSpeed, moveSpeed);
        separationRadius = Mathf.Min(separationRadius, neighborRadius);
    }
}
