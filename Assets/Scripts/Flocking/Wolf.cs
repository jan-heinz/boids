using UnityEngine;
using System.Collections.Generic;
using System.Text;

// runs a simple wolf state machine that hunts weak sheep
[RequireComponent(typeof(Rigidbody2D))]
public class Wolf : MonoBehaviour
{
    private enum WolfState
    {
        Stalk,
        Commit,
        Retreat
    }

    private static readonly List<Wolf> activeWolves = new();

    // -------------------------------------------------------------------------------------------------------------

    [Header("References")]
    [SerializeField]
    [Tooltip("Flock manager used to read the current sheep in the level")]
    private FlockManager flockManager;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    [Tooltip("How fast the wolf can move")]
    private float moveSpeed = 8f;

    [SerializeField]
    [Tooltip("Layers the wolf should treat as blocking obstacles")]
    private LayerMask obstacleLayers = 1 << 6;

    [SerializeField, Min(0f)]
    [Tooltip("How large the wolf collider should be if one needs to be created")]
    private float collisionRadius = 0.45f;

    [SerializeField, Min(0f)]
    [Tooltip("How far the wolf tries to stay from the flock center while stalking")]
    private float stalkDistance = 5f;

    [SerializeField, Min(0f)]
    [Tooltip("How far the wolf backs off after eating or when pushed away")]
    private float retreatDistance = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("How close the wolf needs to get before it considers a move point reached")]
    private float arrivalDistance = 0.15f;

    [SerializeField, Min(0f)]
    [Tooltip("How far ahead the wolf should check for fences")]
    private float obstacleCheckDistance = 1.1f;

    [Header("Hunting")]
    [SerializeField, Min(0f)]
    [Tooltip("How close the wolf must be to eat a sheep")]
    private float eatRadius = 0.9f;

    [SerializeField, Min(0f)]
    [Tooltip("How long the wolf must wait before it can eat again")]
    private float eatCooldown = 3f;

    [SerializeField, Min(0f)]
    [Tooltip("How often the wolf should reevaluate its target")]
    private float retargetInterval = 0.35f;

    [SerializeField]
    [Tooltip("Minimum target score needed before the wolf commits")]
    private float commitThreshold = -0.25f;

    [Header("Targeting")]
    [SerializeField]
    [Tooltip("How much to reward sheep that are far from the flock center")]
    private float stragglerWeight = 1f;

    [SerializeField]
    [Tooltip("How much to punish sheep with nearby allies")]
    private float neighborPenalty = 0.8f;

    [SerializeField]
    [Tooltip("How much to punish sheep that are far from the wolf")]
    private float distancePenalty = 0.08f;

    [SerializeField]
    [Tooltip("How much to reward sheep that are far from any sheepdog")]
    private float sheepdogSafetyWeight = 0.1f;

    [Header("Sheepdog Pressure")]
    [SerializeField, Min(0f)]
    [Tooltip("How close a sheepdog can get before the wolf retreats")]
    private float sheepdogAvoidRadius = 4f;

    [SerializeField]
    [Tooltip("How strongly nearby sheepdogs push the wolf away")]
    private float sheepdogAvoidWeight = 2f;

    private Rigidbody2D rb;
    private Collider2D wolfCollider;
    private WolfState state;
    private SheepAgent targetSheep;
    private Vector2 retreatTarget;
    private float cooldownTimer;
    private float retargetTimer;
    private Vector2 lastPosition;
    private float stuckTime;
    private float lastMovedDistance;
    private float lastTargetScore;
    private Vector2 lastDesiredMoveTarget;
    private Vector2 lastDesiredMoveDirection;
    private Vector2 lastAdjustedMoveDirection;
    private Vector2 lastRetreatDirection;
    private RaycastHit2D lastObstacleHit;
    private RaycastHit2D lastRetreatHit;
    private bool lastRetreatTargetAdjusted;
    private bool lastHoldingRetreatPosition;
    private bool lastSheepdogThreatening;

    public static IReadOnlyList<Wolf> ActiveWolves => activeWolves;

    // caches references and sets up the wolf rigidbody
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        wolfCollider = GetComponent<Collider2D>();

        if (flockManager == null)
        {
            flockManager = FindFirstObjectByType<FlockManager>();
        }

        if (wolfCollider == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = collisionRadius;
            circleCollider.isTrigger = false;
            wolfCollider = circleCollider;
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        state = WolfState.Stalk;
        lastPosition = rb.position;
    }

    // registers this wolf for shared debug logging
    private void OnEnable()
    {
        if (!activeWolves.Contains(this))
        {
            activeWolves.Add(this);
        }
    }

    // removes this wolf when it leaves the scene
    private void OnDisable()
    {
        activeWolves.Remove(this);
    }

    // keeps the auto created collider in a valid range while tuning
    private void OnValidate()
    {
        collisionRadius = Mathf.Max(0.05f, collisionRadius);
        obstacleCheckDistance = Mathf.Max(0f, obstacleCheckDistance);

        if (wolfCollider is CircleCollider2D circleCollider)
        {
            circleCollider.radius = collisionRadius;
        }
    }

    // updates the wolf state and movement each physics step
    private void FixedUpdate()
    {
        if (flockManager == null)
        {
            return;
        }

        ResetDebugState();
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.fixedDeltaTime);
        retargetTimer -= Time.fixedDeltaTime;
        UpdateStuckState();
        lastSheepdogThreatening = IsSheepdogThreatening();

        if (state != WolfState.Retreat && lastSheepdogThreatening)
        {
            EnterRetreatState();
        }

        switch (state)
        {
            case WolfState.Stalk:
                TickStalk();
                break;

            case WolfState.Commit:
                TickCommit();
                break;

            case WolfState.Retreat:
                TickRetreat();
                break;
        }
    }

    // circles outside the flock until a weak target appears
    private void TickStalk()
    {
        if (!TryGetFlockCenter(out Vector2 flockCenter))
        {
            return;
        }

        if (retargetTimer <= 0f || !IsValidTarget(targetSheep))
        {
            targetSheep = FindBestTarget(flockCenter);
            retargetTimer = retargetInterval;
        }

        if (cooldownTimer <= 0f && IsValidTarget(targetSheep))
        {
            float targetScore = GetTargetScore(targetSheep, flockCenter);
            lastTargetScore = targetScore;

            if (targetScore >= commitThreshold)
            {
                state = WolfState.Commit;
                TickCommit();
                return;
            }
        }

        MoveTowards(GetStalkPoint(flockCenter));
    }

    // chases the chosen sheep and eats it if it gets close enough
    private void TickCommit()
    {
        if (cooldownTimer > 0f)
        {
            EnterRetreatState();
            return;
        }

        if (!IsValidTarget(targetSheep))
        {
            state = WolfState.Stalk;
            return;
        }

        if (IsSheepdogThreatening())
        {
            EnterRetreatState();
            return;
        }

        Vector2 targetPosition = targetSheep.transform.position;

        if (CanEatTarget(targetSheep, targetPosition))
        {
            TryEatSheep(targetSheep);
            return;
        }

        lastTargetScore = GetTargetScore(targetSheep, flockManager != null && TryGetFlockCenter(out Vector2 flockCenter) ? flockCenter : rb.position);
        MoveTowards(targetPosition);
    }

    // backs away from danger or after a successful attack
    private void TickRetreat()
    {
        if (cooldownTimer <= 0f && !lastSheepdogThreatening)
        {
            state = WolfState.Stalk;
            return;
        }

        if (stuckTime >= 0.2f)
        {
            lastHoldingRetreatPosition = true;
            return;
        }

        if (Vector2.Distance(rb.position, retreatTarget) > arrivalDistance)
        {
            MoveTowards(retreatTarget, false);
            return;
        }

        if (cooldownTimer > 0f || lastSheepdogThreatening)
        {
            retreatTarget = GetRetreatPoint();
            MoveTowards(retreatTarget, false);
            return;
        }

        state = WolfState.Stalk;
    }

    // picks the sheep that looks easiest to punish
    private SheepAgent FindBestTarget(Vector2 flockCenter)
    {
        IReadOnlyList<SheepAgent> sheep = flockManager.Sheep;
        SheepAgent bestSheep = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < sheep.Count; i++)
        {
            SheepAgent candidate = sheep[i];

            if (!IsValidTarget(candidate))
            {
                continue;
            }

            float score = GetTargetScore(candidate, flockCenter);

            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestSheep = candidate;
        }

        return bestSheep;
    }

    // scores sheep based on how isolated and unsafe they look
    private float GetTargetScore(SheepAgent sheep, Vector2 flockCenter)
    {
        Vector2 sheepPosition = sheep.transform.position;
        float distanceFromCenter = Vector2.Distance(sheepPosition, flockCenter);
        float distanceFromWolf = Vector2.Distance(rb.position, sheepPosition);
        float distanceFromSheepdog = GetNearestSheepdogDistance(sheepPosition);

        return
            (distanceFromCenter * stragglerWeight) -
            (sheep.NeighborCount * neighborPenalty) -
            (distanceFromWolf * distancePenalty) +
            (distanceFromSheepdog * sheepdogSafetyWeight);
    }

    // finds the center of the sheep that are still vulnerable
    private bool TryGetFlockCenter(out Vector2 flockCenter)
    {
        IReadOnlyList<SheepAgent> sheep = flockManager.Sheep;
        Vector2 total = Vector2.zero;
        int count = 0;

        for (int i = 0; i < sheep.Count; i++)
        {
            SheepAgent candidate = sheep[i];

            if (!IsValidTarget(candidate))
            {
                continue;
            }

            total += (Vector2)candidate.transform.position;
            count++;
        }

        if (count == 0)
        {
            flockCenter = rb.position;
            return false;
        }

        flockCenter = total / count;
        return true;
    }

    // keeps the wolf outside the flock instead of standing in the middle of it
    private Vector2 GetStalkPoint(Vector2 flockCenter)
    {
        Vector2 awayFromCenter = rb.position - flockCenter;

        if (awayFromCenter.sqrMagnitude <= 0.0001f)
        {
            awayFromCenter = Vector2.left;
        }

        return flockCenter + (awayFromCenter.normalized * stalkDistance);
    }

    // sends the wolf away from the herd or nearby sheepdogs
    private Vector2 GetRetreatPoint()
    {
        Vector2 retreatDirection = GetSheepdogAvoidanceDirection();

        if (retreatDirection.sqrMagnitude <= 0.0001f && TryGetFlockCenter(out Vector2 flockCenter))
        {
            retreatDirection = rb.position - flockCenter;
        }

        if (retreatDirection.sqrMagnitude <= 0.0001f)
        {
            retreatDirection = Vector2.left;
        }

        lastRetreatDirection = retreatDirection.normalized;
        return GetReachableRetreatPoint(retreatDirection.normalized);
    }

    // keeps the retreat target a little inside the pasture if a fence is in the way
    private Vector2 GetReachableRetreatPoint(Vector2 retreatDirection)
    {
        Vector2 fallbackTarget = rb.position + (retreatDirection * retreatDistance);
        lastRetreatTargetAdjusted = false;
        lastRetreatHit = default;

        if (obstacleLayers.value == 0)
        {
            return fallbackTarget;
        }

        RaycastHit2D hit = Physics2D.CircleCast(
            rb.position,
            collisionRadius,
            retreatDirection,
            retreatDistance,
            obstacleLayers
        );

        if (hit.collider == null)
        {
            return fallbackTarget;
        }

        lastRetreatTargetAdjusted = true;
        lastRetreatHit = hit;
        float safeDistance = Mathf.Max(0f, hit.distance - collisionRadius - arrivalDistance - 0.35f);
        return rb.position + (retreatDirection * safeDistance);
    }

    // moves the wolf toward a world position while also respecting sheepdog pressure
    private void MoveTowards(Vector2 targetPosition, bool allowWallSlide = true)
    {
        Vector2 desiredDirection = targetPosition - rb.position;
        lastDesiredMoveTarget = targetPosition;

        if (desiredDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 sheepdogAvoidance = GetSheepdogAvoidanceDirection() * sheepdogAvoidWeight;
        Vector2 blendedDirection = desiredDirection.normalized + sheepdogAvoidance;

        if (blendedDirection.sqrMagnitude <= 0.0001f)
        {
            blendedDirection = desiredDirection.normalized;
        }

        lastDesiredMoveDirection = blendedDirection.normalized;
        if (allowWallSlide)
        {
            blendedDirection = GetObstacleAwareDirection(blendedDirection.normalized);
        }
        else
        {
            lastObstacleHit = default;
        }
        lastAdjustedMoveDirection = blendedDirection.normalized;
        Vector2 nextPosition = Vector2.MoveTowards(
            rb.position,
            rb.position + blendedDirection.normalized,
            moveSpeed * Time.fixedDeltaTime
        );

        Vector2 moveDelta = nextPosition - rb.position;

        if (obstacleLayers.value != 0 && moveDelta.sqrMagnitude > 0.0001f)
        {
            float moveDistance = moveDelta.magnitude;
            RaycastHit2D moveHit = Physics2D.CircleCast(
                rb.position,
                collisionRadius,
                moveDelta / moveDistance,
                moveDistance + 0.02f,
                obstacleLayers
            );

            if (moveHit.collider != null)
            {
                float safeDistance = Mathf.Max(0f, moveHit.distance - collisionRadius - 0.02f);
                nextPosition = rb.position + ((moveDelta / moveDistance) * safeDistance);
            }
        }

        rb.MovePosition(nextPosition);
    }

    // steers the wolf off fence normals instead of running straight into them
    private Vector2 GetObstacleAwareDirection(Vector2 desiredDirection)
    {
        lastObstacleHit = default;

        if (desiredDirection.sqrMagnitude <= 0.0001f || obstacleLayers.value == 0)
        {
            return desiredDirection;
        }

        RaycastHit2D hit = Physics2D.CircleCast(
            rb.position,
            collisionRadius,
            desiredDirection,
            obstacleCheckDistance,
            obstacleLayers
        );

        if (hit.collider == null || hit.normal.sqrMagnitude <= 0.0001f)
        {
            return desiredDirection;
        }

        lastObstacleHit = hit;
        Vector2 wallNormal = hit.normal.normalized;
        Vector2 tangent = Vector2.Perpendicular(wallNormal).normalized;

        if (Vector2.Dot(tangent, desiredDirection) < 0f)
        {
            tangent *= -1f;
        }

        Vector2 blendedDirection = (wallNormal * 1.35f) + (tangent * 0.85f);
        return blendedDirection.sqrMagnitude > 0.0001f
            ? blendedDirection.normalized
            : wallNormal;
    }

    // tracks when the wolf is pushing in place so it can recover
    private void UpdateStuckState()
    {
        float movedDistance = Vector2.Distance(rb.position, lastPosition);
        lastMovedDistance = movedDistance;

        if (movedDistance <= 0.01f)
        {
            stuckTime += Time.fixedDeltaTime;
        }
        else
        {
            stuckTime = 0f;
        }

        lastPosition = rb.position;
    }

    // clears one frame of wolf debug state before new decisions are made
    private void ResetDebugState()
    {
        lastHoldingRetreatPosition = false;
        lastObstacleHit = default;
        lastRetreatHit = default;
        lastRetreatTargetAdjusted = false;
        lastDesiredMoveTarget = rb != null ? rb.position : (Vector2)transform.position;
        lastDesiredMoveDirection = Vector2.zero;
        lastAdjustedMoveDirection = Vector2.zero;
        lastRetreatDirection = Vector2.zero;
        lastTargetScore = 0f;
    }

    // checks whether a sheepdog is close enough to force a retreat
    private bool IsSheepdogThreatening()
    {
        IReadOnlyList<Sheepdog> sheepdogs = Sheepdog.ActiveSheepdogs;

        for (int i = 0; i < sheepdogs.Count; i++)
        {
            Sheepdog sheepdog = sheepdogs[i];

            if (sheepdog == null || !sheepdog.IsPlaced)
            {
                continue;
            }

            if (Vector2.Distance(rb.position, sheepdog.transform.position) <= sheepdogAvoidRadius)
            {
                return true;
            }
        }

        return false;
    }

    // computes a direction that moves the wolf away from nearby sheepdogs
    private Vector2 GetSheepdogAvoidanceDirection()
    {
        IReadOnlyList<Sheepdog> sheepdogs = Sheepdog.ActiveSheepdogs;
        Vector2 avoidance = Vector2.zero;

        for (int i = 0; i < sheepdogs.Count; i++)
        {
            Sheepdog sheepdog = sheepdogs[i];

            if (sheepdog == null || !sheepdog.IsPlaced)
            {
                continue;
            }

            Vector2 awayFromSheepdog = rb.position - (Vector2)sheepdog.transform.position;
            float distance = awayFromSheepdog.magnitude;

            if (distance <= 0.0001f || distance > sheepdogAvoidRadius)
            {
                continue;
            }

            float pressure = 1f - Mathf.Clamp01(distance / sheepdogAvoidRadius);
            avoidance += awayFromSheepdog.normalized * pressure;
        }

        return avoidance;
    }

    // finds how protected a sheep is by the nearest sheepdog
    private float GetNearestSheepdogDistance(Vector2 sheepPosition)
    {
        IReadOnlyList<Sheepdog> sheepdogs = Sheepdog.ActiveSheepdogs;
        float nearestDistance = 0f;
        bool foundSheepdog = false;

        for (int i = 0; i < sheepdogs.Count; i++)
        {
            Sheepdog sheepdog = sheepdogs[i];

            if (sheepdog == null || !sheepdog.IsPlaced)
            {
                continue;
            }

            float distance = Vector2.Distance(sheepPosition, sheepdog.transform.position);

            if (!foundSheepdog || distance < nearestDistance)
            {
                nearestDistance = distance;
                foundSheepdog = true;
            }
        }

        return foundSheepdog ? nearestDistance : sheepdogAvoidRadius;
    }

    // checks whether the wolf has physically reached the target sheep
    private bool CanEatTarget(SheepAgent sheep, Vector2 sheepPosition)
    {
        if (sheep == null)
        {
            return false;
        }

        float bodyContactDistance = collisionRadius + sheep.CollisionRadius;
        float allowedEatDistance = Mathf.Max(eatRadius, bodyContactDistance);
        return Vector2.Distance(rb.position, sheepPosition) <= allowedEatDistance;
    }

    // eats a sheep if the wolf is allowed to do so right now
    private bool TryEatSheep(SheepAgent sheep)
    {
        if (cooldownTimer > 0f || !IsValidTarget(sheep))
        {
            return false;
        }

        sheep.BeEaten();
        targetSheep = null;
        cooldownTimer = eatCooldown;
        EnterRetreatState();
        return true;
    }

    // lets direct physical contact with a sheep count as an eat
    private void OnCollisionEnter2D(Collision2D collision)
    {
        SheepAgent sheep = collision.collider.GetComponent<SheepAgent>();
        TryEatSheep(sheep);
    }

    // keeps the eat reliable even if the wolf is already pressing into the sheep
    private void OnCollisionStay2D(Collision2D collision)
    {
        SheepAgent sheep = collision.collider.GetComponent<SheepAgent>();
        TryEatSheep(sheep);
    }

    // moves the wolf into its retreat state
    private void EnterRetreatState()
    {
        state = WolfState.Retreat;
        retreatTarget = GetRetreatPoint();
        targetSheep = null;
    }

    // validates that a sheep is still a real target
    private bool IsValidTarget(SheepAgent sheep)
    {
        return sheep != null && sheep.CanBeTargeted;
    }

    // writes this wolf's current debug snapshot into the shared log entry
    public void AppendDebugLogEntry(StringBuilder entry, string indent)
    {
        entry.AppendLine($"{indent}{name}");

        if (BoidDebugPanel.LogWolfBehavior && BoidDebugPanel.LogWolfMovementState)
        {
            entry.AppendLine($"{indent}  movement");
            entry.AppendLine($"{indent}    pos: {FormatVector(rb.position)}");
            entry.AppendLine($"{indent}    moved: {lastMovedDistance:0.000}");
            entry.AppendLine($"{indent}    desired target: {FormatVector(lastDesiredMoveTarget)}");
            entry.AppendLine($"{indent}    desired dir: {FormatVector(lastDesiredMoveDirection)}");
            entry.AppendLine($"{indent}    adjusted dir: {FormatVector(lastAdjustedMoveDirection)}");
        }

        if (BoidDebugPanel.LogWolfBehavior && BoidDebugPanel.LogWolfTargetState)
        {
            entry.AppendLine($"{indent}  targeting");
            entry.AppendLine($"{indent}    state: {state}");
            entry.AppendLine($"{indent}    target: {(targetSheep != null ? targetSheep.name : "none")}");
            entry.AppendLine($"{indent}    target score: {lastTargetScore:0.00}");
            entry.AppendLine($"{indent}    sheepdog threat: {lastSheepdogThreatening}");
        }

        if (BoidDebugPanel.LogWolfBehavior && BoidDebugPanel.LogWolfRetreatState)
        {
            entry.AppendLine($"{indent}  retreat");
            entry.AppendLine($"{indent}    retreat target: {FormatVector(retreatTarget)}");
            entry.AppendLine($"{indent}    retreat dir: {FormatVector(lastRetreatDirection)}");
            entry.AppendLine($"{indent}    cooldown: {cooldownTimer:0.00}");
            entry.AppendLine($"{indent}    stuck time: {stuckTime:0.000}");
            entry.AppendLine($"{indent}    clamped target: {lastRetreatTargetAdjusted}");
            entry.AppendLine($"{indent}    holding position: {lastHoldingRetreatPosition}");
        }

        if (BoidDebugPanel.LogWolfBehavior && BoidDebugPanel.LogWolfObstacleState)
        {
            entry.AppendLine($"{indent}  obstacle");
            entry.AppendLine($"{indent}    move hit: {FormatHit(lastObstacleHit)}");
            entry.AppendLine($"{indent}    retreat hit: {FormatHit(lastRetreatHit)}");
        }
    }

    // checks whether this wolf has active debug data worth writing right now
    public bool HasActiveDebugState()
    {
        return targetSheep != null
            || cooldownTimer > 0.0001f
            || stuckTime > 0.0001f
            || lastObstacleHit.collider != null
            || lastRetreatHit.collider != null
            || lastRetreatTargetAdjusted
            || lastHoldingRetreatPosition
            || lastDesiredMoveDirection.sqrMagnitude > 0.0001f
            || lastAdjustedMoveDirection.sqrMagnitude > 0.0001f;
    }

    // formats a hit for wolf debug output
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

    // draws a few tuning ranges in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawSphere(transform.position, eatRadius);
        Gizmos.color = new Color(1f, 0.5f, 0.15f, 0.25f);
        Gizmos.DrawSphere(transform.position, sheepdogAvoidRadius);
        Gizmos.color = new Color(0.9f, 0.3f, 0.9f, 0.2f);
        Gizmos.DrawSphere(transform.position, stalkDistance);
    }
}
