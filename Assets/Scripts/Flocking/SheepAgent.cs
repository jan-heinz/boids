using UnityEngine;
using System.Collections.Generic;
using System.Text;

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
    private readonly List<SheepAgent> neighbors = new();
    private Rigidbody2D rb;
    private Collider2D sheepCollider;
    private FlockManager flockManager;
    private GoalArea currentGoalArea;
    private int lastNeighborCount;
    private float lastBoidTurnAngle;
    private float lastBoidCancellationRatio;
    private Vector2 lastSeparationForce;
    private Vector2 lastAlignmentForce;
    private Vector2 lastCohesionForce;
    private Vector2 lastBoidSteeringForce;
    private Vector2 lastPosition;
    private float stuckTime;
    private float lastMovedDistance;
    private RaycastHit2D lastFrontHit;
    private RaycastHit2D lastLeftHit;
    private RaycastHit2D lastRightHit;
    private Vector2 lastCombinedNormal;
    private int lastDistinctNormalCount;
    private float lastFrontPressure;
    private float lastLeftPressure;
    private float lastRightPressure;
    private float lastWallPressure;
    private bool lastShouldEscapeCorner;
    private Vector2 lastDesiredDirection;
    private Vector2 lastSteeringForce;

    public Vector2 CurrentVelocity => currentVelocity;
    public IReadOnlyList<SheepAgent> Neighbors => neighbors;
    public int NeighborCount => neighbors.Count;
    public float CollisionRadius => sheepCollider != null
        ? Mathf.Max(sheepCollider.bounds.extents.x, sheepCollider.bounds.extents.y)
        : 0.5f;
    public bool IsSafe => currentGoalArea != null;
    public bool CanBeTargeted => isActiveAndEnabled && currentGoalArea == null;

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

    // removes this sheep after a wolf catches it
    public void BeEaten()
    {
        Destroy(gameObject);
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
        lastPosition = rb.position;
    }

    // steers and moves the sheep during the physics step
    private void FixedUpdate()
    {
        float movedDistance = Vector2.Distance(rb.position, lastPosition);
        ResetBoidDebugState();
        UpdateNeighbors();
        if (currentGoalArea != null)
        {
            ApplyGoalAreaBehavior();
        }
        else
        {
            ApplyBoidBehaviors();
        }

        bool isAvoidingObstacle = ApplyObstacleAvoidance(movedDistance);
        ClampSpeed();

        if (currentGoalArea != null)
        {
            ClampGoalAreaSpeed();
        }
        else if (!isAvoidingObstacle)
        {
            MaintainMoveSpeed();
        }

        rb.linearVelocity = currentVelocity;
        lastPosition = rb.position;
    }

    // combines boid forces and clamps them once so the total respects the steering limit
    private void ApplyBoidBehaviors()
    {
        if (settings == null)
        {
            return;
        }

        Vector2 separationForce = neighbors.Count > 0 ? ComputeSeparationForce() : Vector2.zero;
        Vector2 alignmentForce = neighbors.Count > 0 ? ComputeAlignmentForce() : Vector2.zero;
        Vector2 cohesionForce = neighbors.Count > 0 ? ComputeCohesionForce() : Vector2.zero;
        Vector2 sheepdogForce = ComputeSheepdogForce();
        Vector2 totalForce = separationForce + alignmentForce + cohesionForce;
        Vector2 rawTotalForce = totalForce;
        float totalComponentMagnitude =
            separationForce.magnitude +
            alignmentForce.magnitude +
            cohesionForce.magnitude;
        float maxSteeringStep = settings.MaxSteeringForce * Time.fixedDeltaTime;
        Vector2 startingVelocity = currentVelocity;
        Vector2 combinedForce = totalForce + sheepdogForce;

        if (combinedForce.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        if (combinedForce.magnitude > maxSteeringStep && maxSteeringStep > 0f)
        {
            float scale = maxSteeringStep / combinedForce.magnitude;
            separationForce *= scale;
            alignmentForce *= scale;
            cohesionForce *= scale;
            sheepdogForce *= scale;
            totalForce *= scale;
            combinedForce *= scale;
        }

        lastBoidTurnAngle = GetSignedAngle(startingVelocity, startingVelocity + totalForce);
        lastBoidCancellationRatio = GetCancellationRatio(rawTotalForce, totalComponentMagnitude);
        lastSeparationForce = separationForce;
        lastAlignmentForce = alignmentForce;
        lastCohesionForce = cohesionForce;
        lastBoidSteeringForce = totalForce;
        currentVelocity += combinedForce;
    }

    // pushes the sheep away from nearby neighbors that are too close
    private Vector2 ComputeSeparationForce()
    {
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
            return Vector2.zero;
        }

        Vector2 desiredVelocity = separationDirection.normalized * settings.MoveSpeed;
        return (desiredVelocity - currentVelocity) * settings.SeparationWeight;
    }

    // steers the sheep toward the average heading of nearby neighbors
    private Vector2 ComputeAlignmentForce()
    {
        Vector2 averageHeading = Vector2.zero;

        for (int i = 0; i < neighbors.Count; i++)
        {
            SheepAgent neighbor = neighbors[i];

            if (neighbor == null || neighbor.currentVelocity.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            averageHeading += neighbor.currentVelocity.normalized;
        }

        if (averageHeading.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        Vector2 desiredVelocity = averageHeading.normalized * settings.MoveSpeed;
        return (desiredVelocity - currentVelocity) * settings.AlignmentWeight;
    }

    // steers the sheep toward the local center of nearby neighbors
    private Vector2 ComputeCohesionForce()
    {
        Vector2 neighborCenter = Vector2.zero;

        for (int i = 0; i < neighbors.Count; i++)
        {
            SheepAgent neighbor = neighbors[i];

            if (neighbor == null)
            {
                continue;
            }

            neighborCenter += neighbor.rb.position;
        }

        neighborCenter /= neighbors.Count;

        Vector2 toNeighborCenter = neighborCenter - rb.position;

        if (toNeighborCenter.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        Vector2 desiredVelocity = toNeighborCenter.normalized * settings.MoveSpeed;
        return (desiredVelocity - currentVelocity) * settings.CohesionWeight;
    }

    // pushes sheep away from any placed sheepdogs that are nearby
    private Vector2 ComputeSheepdogForce()
    {
        IReadOnlyList<Sheepdog> sheepdogs = Sheepdog.ActiveSheepdogs;

        if (sheepdogs.Count == 0)
        {
            return Vector2.zero;
        }

        Vector2 pressureDirection = Vector2.zero;
        Vector2 position = rb.position;

        for (int i = 0; i < sheepdogs.Count; i++)
        {
            Sheepdog sheepdog = sheepdogs[i];

            if (sheepdog == null || !sheepdog.IsPlaced)
            {
                continue;
            }

            Vector2 awayFromDog = position - (Vector2)sheepdog.transform.position;
            float distance = awayFromDog.magnitude;

            if (distance > sheepdog.InfluenceRadius)
            {
                continue;
            }

            if (distance <= 0.0001f)
            {
                awayFromDog = GetEmergencyScatterDirection();
                distance = 0f;
            }

            float pressure = sheepdog.GetPressure(distance);
            pressureDirection += awayFromDog.normalized * pressure;
        }

        if (pressureDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        Vector2 desiredVelocity = pressureDirection.normalized * settings.MoveSpeed;
        return (desiredVelocity - currentVelocity) * pressureDirection.magnitude;
    }

    // settles sheep down after they reach the goal area
    private void ApplyGoalAreaBehavior()
    {
        if (settings == null || currentGoalArea == null)
        {
            return;
        }

        Vector2 centerDirection = currentGoalArea.GetCenterDirection(rb.position);
        float stopRadius = currentGoalArea.StopRadius;
        float maxSteeringStep = settings.MaxSteeringForce * Time.fixedDeltaTime;
        Vector2 toGoalCenter = currentGoalArea.Center - rb.position;

        if (toGoalCenter.sqrMagnitude <= stopRadius * stopRadius)
        {
            currentVelocity = Vector2.MoveTowards(
                currentVelocity,
                Vector2.zero,
                currentGoalArea.SettleBrake * Time.fixedDeltaTime
            );
            return;
        }

        Vector2 desiredVelocity = centerDirection * settings.MoveSpeed * currentGoalArea.SettleSpeedMultiplier;
        Vector2 totalForce = desiredVelocity - currentVelocity;

        if (totalForce.magnitude > maxSteeringStep && maxSteeringStep > 0f)
        {
            totalForce = totalForce.normalized * maxSteeringStep;
        }

        currentVelocity += totalForce;
    }

    // rebuilds the nearby sheep list using the shared neighbor radius
    private void UpdateNeighbors()
    {
        neighbors.Clear();
        lastNeighborCount = 0;

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

        lastNeighborCount = neighbors.Count;
    }

    // clears the per frame boid debug snapshot before new forces are applied
    private void ResetBoidDebugState()
    {
        lastNeighborCount = 0;
        lastBoidTurnAngle = 0f;
        lastBoidCancellationRatio = 0f;
        lastSeparationForce = Vector2.zero;
        lastAlignmentForce = Vector2.zero;
        lastCohesionForce = Vector2.zero;
        lastBoidSteeringForce = Vector2.zero;
    }

    // predicts fence hits ahead of the sheep and redirects movement before contact
    private bool ApplyObstacleAvoidance(float movedDistance)
    {
        Vector2 position = rb.position;
        Vector2 forward = currentVelocity.sqrMagnitude > 0.0001f
            ? currentVelocity.normalized
            : GetStartingDirection();
        float castRadius = GetCastRadius();
        float probeDistance = settings.ObstacleCheckRadius;

        RaycastHit2D frontHit = Physics2D.CircleCast(position, castRadius, forward, probeDistance, obstacleLayers);
        RaycastHit2D leftHit = Physics2D.CircleCast(position, castRadius, Rotate(forward, 35f), probeDistance, obstacleLayers);
        RaycastHit2D rightHit = Physics2D.CircleCast(position, castRadius, Rotate(forward, -35f), probeDistance, obstacleLayers);

        lastMovedDistance = movedDistance;
        lastFrontHit = frontHit;
        lastLeftHit = leftHit;
        lastRightHit = rightHit;

        if (frontHit.collider == null && leftHit.collider == null && rightHit.collider == null)
        {
            stuckTime = 0f;
            lastCombinedNormal = Vector2.zero;
            lastDistinctNormalCount = 0;
            lastFrontPressure = 0f;
            lastLeftPressure = 0f;
            lastRightPressure = 0f;
            lastWallPressure = 0f;
            lastShouldEscapeCorner = false;
            lastDesiredDirection = Vector2.zero;
            lastSteeringForce = Vector2.zero;
            return false;
        }

        if (movedDistance <= 0.01f && currentVelocity.sqrMagnitude > 0.01f)
        {
            stuckTime += Time.fixedDeltaTime;
        }
        else
        {
            stuckTime = 0f;
        }

        Vector2 combinedNormal = Vector2.zero;
        Vector2 firstNormal = Vector2.zero;
        int distinctNormalCount = 0;

        AddObstacleNormal(frontHit, ref combinedNormal, ref firstNormal, ref distinctNormalCount);
        AddObstacleNormal(leftHit, ref combinedNormal, ref firstNormal, ref distinctNormalCount);
        AddObstacleNormal(rightHit, ref combinedNormal, ref firstNormal, ref distinctNormalCount);

        if (combinedNormal.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        Vector2 wallNormal = combinedNormal.normalized;
        float frontPressure = GetHitPressure(frontHit, probeDistance);
        float leftPressure = GetHitPressure(leftHit, probeDistance);
        float rightPressure = GetHitPressure(rightHit, probeDistance);
        float wallPressure = Mathf.Max(frontPressure, Mathf.Max(leftPressure, rightPressure));
        Vector2 steeringNormal =
            (frontHit.collider != null ? frontHit.normal * frontPressure : Vector2.zero) +
            (leftHit.collider != null ? leftHit.normal * leftPressure : Vector2.zero) +
            (rightHit.collider != null ? rightHit.normal * rightPressure : Vector2.zero);

        if (steeringNormal.sqrMagnitude <= 0.0001f)
        {
            steeringNormal = wallNormal;
        }
        else
        {
            steeringNormal.Normalize();
        }

        bool shouldEscapeCorner = distinctNormalCount >= 2 || stuckTime >= 0.2f;
        Vector2 desiredDirection;

        if (shouldEscapeCorner)
        {
            // corners use the combined normals as a clear escape direction
            desiredDirection = ((steeringNormal * 1.5f) + (forward * 0.1f)).normalized;
        }
        else
        {
            // single walls blend the current heading with a growing outward push for a smoother curve away
            Vector2 outwardBias = steeringNormal * Mathf.Lerp(0.15f, 1.1f, wallPressure);
            desiredDirection = (forward + outwardBias).normalized;

            if (desiredDirection.sqrMagnitude <= 0.0001f)
            {
                desiredDirection = steeringNormal;
            }
        }

        Vector2 desiredVelocity = desiredDirection.normalized * settings.MoveSpeed;
        Vector2 steeringForce = desiredVelocity - currentVelocity;
        float maxSteeringStep = settings.MaxSteeringForce * Time.fixedDeltaTime * (shouldEscapeCorner ? 1.75f : 1f);
        steeringForce = Vector2.ClampMagnitude(steeringForce, maxSteeringStep);

        lastCombinedNormal = wallNormal;
        lastDistinctNormalCount = distinctNormalCount;
        lastFrontPressure = frontPressure;
        lastLeftPressure = leftPressure;
        lastRightPressure = rightPressure;
        lastWallPressure = wallPressure;
        lastShouldEscapeCorner = shouldEscapeCorner;
        lastDesiredDirection = desiredDirection;
        lastSteeringForce = steeringForce;

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

    // keeps sheep moving slowly once they have reached the goal area
    private void ClampGoalAreaSpeed()
    {
        if (currentGoalArea == null)
        {
            return;
        }

        float goalSpeed = settings.MoveSpeed * currentGoalArea.SettleSpeedMultiplier;
        currentVelocity = Vector2.ClampMagnitude(currentVelocity, goalSpeed);
    }

    // uses the chosen start direction when valid otherwise falls back to the right
    private Vector2 GetStartingDirection()
    {
        return startingDirection.sqrMagnitude > 0f
            ? startingDirection.normalized
            : Vector2.right;
    }

    // picks a reasonable cast radius from the sheep collider size
    private float GetCastRadius()
    {
        Bounds bounds = sheepCollider.bounds;
        return Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.y));
    }

    // collects obstacle normals and counts when the sheep is reading a corner instead of a single wall
    private void AddObstacleNormal(RaycastHit2D hit, ref Vector2 combinedNormal, ref Vector2 firstNormal, ref int distinctNormalCount)
    {
        if (hit.collider == null || hit.normal.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 normal = hit.normal.normalized;

        if (distinctNormalCount == 0)
        {
            firstNormal = normal;
            distinctNormalCount = 1;
        }
        else if (Vector2.Dot(firstNormal, normal) < 0.85f)
        {
            distinctNormalCount = 2;
        }

        combinedNormal += normal;
    }

    // converts a cast hit into a 0 to 1 pressure value based on how close the fence is
    private float GetHitPressure(RaycastHit2D hit, float probeDistance)
    {
        if (hit.collider == null || probeDistance <= 0f)
        {
            return 0f;
        }

        return 1f - Mathf.Clamp01(hit.distance / probeDistance);
    }

    // picks a fallback direction when the sheepdog is placed directly on a sheep
    private Vector2 GetEmergencyScatterDirection()
    {
        if (currentVelocity.sqrMagnitude > 0.0001f)
        {
            return currentVelocity.normalized;
        }

        float angle = Mathf.Abs(GetInstanceID() * 0.137f) % 360f;
        return Rotate(Vector2.right, angle).normalized;
    }

    // rotates a 2d direction by the given angle in degrees
    private Vector2 Rotate(Vector2 direction, float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(angleRadians);
        float cos = Mathf.Cos(angleRadians);

        return new Vector2(
            (direction.x * cos) - (direction.y * sin),
            (direction.x * sin) + (direction.y * cos)
        );
    }

    // enters the settle behavior when the sheep reaches a goal area
    private void OnTriggerEnter2D(Collider2D other)
    {
        GoalArea goalArea = other.GetComponent<GoalArea>();

        if (goalArea == null)
        {
            return;
        }

        goalArea.RegisterSheep(this);
        currentGoalArea = goalArea;
    }

    // returns to normal flocking if the sheep leaves the goal area
    private void OnTriggerExit2D(Collider2D other)
    {
        GoalArea goalArea = other.GetComponent<GoalArea>();

        if (goalArea == null || currentGoalArea != goalArea)
        {
            return;
        }

        currentGoalArea = null;
    }

    // appends this sheep's current debug snapshot into the shared log entry
    public void AppendDebugLogEntry(StringBuilder entry, string indent)
    {
        entry.AppendLine($"{indent}{name}");

        if (BoidDebugPanel.LogBoidBehavior && BoidDebugPanel.LogMovementState)
        {
            Vector2 forward = currentVelocity.sqrMagnitude > 0.0001f
                ? currentVelocity.normalized
                : GetStartingDirection();

            entry.AppendLine($"{indent}  movement");
            entry.AppendLine($"{indent}    pos: {FormatVector(rb.position)}");
            entry.AppendLine($"{indent}    vel: {FormatVector(currentVelocity)}");
            entry.AppendLine($"{indent}    forward: {FormatVector(forward)}");
            entry.AppendLine($"{indent}    moved: {lastMovedDistance:0.000}");
        }

        if (BoidDebugPanel.LogFenceBehavior
            && (BoidDebugPanel.LogFrontHit || BoidDebugPanel.LogLeftHit || BoidDebugPanel.LogRightHit))
        {
            entry.AppendLine($"{indent}  hits");
        }

        if (BoidDebugPanel.LogFenceBehavior && BoidDebugPanel.LogFrontHit)
        {
            entry.AppendLine($"{indent}    front: {FormatHit(lastFrontHit)}");
        }

        if (BoidDebugPanel.LogFenceBehavior && BoidDebugPanel.LogLeftHit)
        {
            entry.AppendLine($"{indent}    left: {FormatHit(lastLeftHit)}");
        }

        if (BoidDebugPanel.LogFenceBehavior && BoidDebugPanel.LogRightHit)
        {
            entry.AppendLine($"{indent}    right: {FormatHit(lastRightHit)}");
        }

        if (BoidDebugPanel.LogFenceBehavior
            && (BoidDebugPanel.LogCombinedNormal
                || BoidDebugPanel.LogWallPressure
                || BoidDebugPanel.LogCornerState
                || BoidDebugPanel.LogStuckTime
                || BoidDebugPanel.LogDesiredDirection
                || BoidDebugPanel.LogSteeringForce))
        {
            entry.AppendLine($"{indent}  obstacle response");
        }

        if (BoidDebugPanel.LogFenceBehavior && BoidDebugPanel.LogCombinedNormal)
        {
            entry.AppendLine($"{indent}    combined normal: {FormatVector(lastCombinedNormal)}");
            entry.AppendLine($"{indent}    distinct normals: {lastDistinctNormalCount}");
        }

        if (BoidDebugPanel.LogFenceBehavior && BoidDebugPanel.LogWallPressure)
        {
            entry.AppendLine($"{indent}    pressure front: {lastFrontPressure:0.000}");
            entry.AppendLine($"{indent}    pressure left: {lastLeftPressure:0.000}");
            entry.AppendLine($"{indent}    pressure right: {lastRightPressure:0.000}");
            entry.AppendLine($"{indent}    pressure max: {lastWallPressure:0.000}");
        }

        if (BoidDebugPanel.LogFenceBehavior && BoidDebugPanel.LogCornerState)
        {
            entry.AppendLine($"{indent}    escape corner: {lastShouldEscapeCorner}");
        }

        if (BoidDebugPanel.LogFenceBehavior && BoidDebugPanel.LogStuckTime)
        {
            entry.AppendLine($"{indent}    stuck time: {stuckTime:0.000}");
        }

        if (BoidDebugPanel.LogFenceBehavior && BoidDebugPanel.LogDesiredDirection)
        {
            entry.AppendLine($"{indent}    desired direction: {FormatVector(lastDesiredDirection)}");
        }

        if (BoidDebugPanel.LogFenceBehavior && BoidDebugPanel.LogSteeringForce)
        {
            entry.AppendLine($"{indent}    steering force: {FormatVector(lastSteeringForce)}");
        }

        if (BoidDebugPanel.LogBoidBehavior
            && (BoidDebugPanel.LogNeighborState
                || BoidDebugPanel.LogBoidSummary
                || BoidDebugPanel.LogSeparationForce
                || BoidDebugPanel.LogAlignmentForce
                || BoidDebugPanel.LogCohesionForce
                || BoidDebugPanel.LogBoidSteeringForce))
        {
            entry.AppendLine($"{indent}  boid response");
        }

        if (BoidDebugPanel.LogBoidBehavior && BoidDebugPanel.LogNeighborState)
        {
            entry.AppendLine($"{indent}    neighbors: {lastNeighborCount}");
        }

        if (BoidDebugPanel.LogBoidBehavior && BoidDebugPanel.LogBoidSummary)
        {
            entry.AppendLine(
                $"{indent}    summary: turn={lastBoidTurnAngle:0.0}deg cancel={lastBoidCancellationRatio:0.00}");
        }

        if (BoidDebugPanel.LogBoidBehavior && BoidDebugPanel.LogSeparationForce)
        {
            entry.AppendLine($"{indent}    separation force: {FormatVector(lastSeparationForce)}");
        }

        if (BoidDebugPanel.LogBoidBehavior && BoidDebugPanel.LogAlignmentForce)
        {
            entry.AppendLine($"{indent}    alignment force: {FormatVector(lastAlignmentForce)}");
        }

        if (BoidDebugPanel.LogBoidBehavior && BoidDebugPanel.LogCohesionForce)
        {
            entry.AppendLine($"{indent}    cohesion force: {FormatVector(lastCohesionForce)}");
        }

        if (BoidDebugPanel.LogBoidBehavior && BoidDebugPanel.LogBoidSteeringForce)
        {
            entry.AppendLine($"{indent}    boid steering force: {FormatVector(lastBoidSteeringForce)}");
        }
    }

    // checks whether this sheep has obstacle data worth writing right now
    public bool HasActiveObstacleDebugState()
    {
        return lastFrontHit.collider != null
            || lastLeftHit.collider != null
            || lastRightHit.collider != null
            || lastDistinctNormalCount > 0
            || lastFrontPressure > 0.0001f
            || lastLeftPressure > 0.0001f
            || lastRightPressure > 0.0001f
            || lastWallPressure > 0.0001f
            || lastShouldEscapeCorner
            || stuckTime > 0.0001f
            || lastDesiredDirection.sqrMagnitude > 0.0001f
            || lastSteeringForce.sqrMagnitude > 0.0001f;
    }

    // checks whether this sheep has boid data worth writing right now
    public bool HasActiveBoidDebugState()
    {
        return lastNeighborCount > 0
            || lastSeparationForce.sqrMagnitude > 0.0001f
            || lastAlignmentForce.sqrMagnitude > 0.0001f
            || lastCohesionForce.sqrMagnitude > 0.0001f
            || lastBoidSteeringForce.sqrMagnitude > 0.0001f;
    }

    // formats a cast hit for debug output
    private string FormatHit(RaycastHit2D hit)
    {
        if (hit.collider == null)
        {
            return "none";
        }

        return $"dist={hit.distance:0.000} normal={FormatVector(hit.normal)} collider={hit.collider.name}";
    }

    // formats a vector into a short plain text form for the log file
    private string FormatVector(Vector2 value)
    {
        return $"({value.x:0.00}, {value.y:0.00})";
    }

    // measures how much the boid steering changed the current heading
    private float GetSignedAngle(Vector2 fromVelocity, Vector2 toVelocity)
    {
        if (fromVelocity.sqrMagnitude <= 0.0001f || toVelocity.sqrMagnitude <= 0.0001f)
        {
            return 0f;
        }

        return Vector2.SignedAngle(fromVelocity, toVelocity);
    }

    // estimates how much the boid forces cancelled each other before the final clamp
    private float GetCancellationRatio(Vector2 totalForce, float totalComponentMagnitude)
    {
        if (totalComponentMagnitude <= 0.0001f)
        {
            return 0f;
        }

        return Mathf.Clamp01(1f - (totalForce.magnitude / totalComponentMagnitude));
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
