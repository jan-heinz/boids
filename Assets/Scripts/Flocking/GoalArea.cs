using UnityEngine;
using System.Collections.Generic;

// marks a safe area where sheep slow down and settle after arriving
[RequireComponent(typeof(BoxCollider2D))]
public class GoalArea : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [Header("Settling")]
    [SerializeField, Min(0f)]
    [Tooltip("How quickly sheep slow down after entering the goal area")]
    private float settleBrake = 12f;

    [SerializeField, Min(0f)]
    [Tooltip("Top speed sheep can use while moving to the center")]
    private float settleSpeedMultiplier = 0.4f;

    [SerializeField, Min(0f)]
    [Tooltip("How close sheep should get to the center before they stop")]
    private float stopRadius = 0.75f;

    [SerializeField]
    [Tooltip("Optional flock manager used to read the total sheep count")]
    private FlockManager flockManager;

    private BoxCollider2D goalCollider;
    private readonly HashSet<SheepAgent> safeSheep = new();

    public float SettleBrake => settleBrake;
    public float SettleSpeedMultiplier => settleSpeedMultiplier;
    public float StopRadius => stopRadius;
    public Vector2 Center => goalCollider != null ? goalCollider.bounds.center : transform.position;
    public int SafeSheepCount => safeSheep.Count;
    public int TotalSheepCount => flockManager != null ? flockManager.SheepCount : 0;
    public string ScoreText => $"{SafeSheepCount}/{TotalSheepCount}";

    // caches the trigger collider used by the goal area
    private void Awake()
    {
        goalCollider = GetComponent<BoxCollider2D>();

        if (goalCollider != null && !goalCollider.isTrigger)
        {
            Debug.LogWarning("GoalArea works best when its BoxCollider2D is set as a trigger", this);
        }

        if (flockManager == null)
        {
            flockManager = FindFirstObjectByType<FlockManager>();
        }
    }

    // returns a direction from a sheep toward the center of the goal area
    public Vector2 GetCenterDirection(Vector2 position)
    {
        if (goalCollider == null)
        {
            return Vector2.zero;
        }

        Vector2 toTarget = Center - position;

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        return toTarget.normalized;
    }

    // counts a sheep as safe the first time it reaches the goal area
    public void RegisterSheep(SheepAgent sheep)
    {
        if (sheep == null)
        {
            return;
        }

        safeSheep.Add(sheep);
    }
}
