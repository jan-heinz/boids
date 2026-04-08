using UnityEngine;
using System.Collections.Generic;
using System.Text;

// runs a simple wolf state machine that hunts weak sheep
[RequireComponent(typeof(Rigidbody2D))]
public class Wolf : MonoBehaviour
{
    public enum VisualState
    {
        Stalk,
        Commit,
        Retreat
    }

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
    [Tooltip("How much room the wolf has to drift inward or outward while stalking")]
    private float stalkBand = 1.2f;

    [SerializeField, Min(0f)]
    [Tooltip("How often the wolf changes stalking direction around the flock")]
    private float stalkDirectionChangeInterval = 1.6f;

    [SerializeField, Min(0f)]
    [Tooltip("How long the wolf should move before pausing during stalking")]
    private float stalkMoveDuration = 0.65f;

    [SerializeField, Min(0f)]
    [Tooltip("How long the wolf should pause between stalking moves")]
    private float stalkHoldDuration = 1.1f;

    [SerializeField, Min(0f)]
    [Tooltip("How far ahead the wolf aims around the flock while stalking")]
    private float stalkLeadDistance = 3f;

    [SerializeField, Min(0f)]
    [Tooltip("How much extra fence margin the wolf keeps while stalking")]
    private float stalkFencePadding = 0.35f;

    [SerializeField, Min(0f)]
    [Tooltip("How far the wolf backs off after eating or when pushed away")]
    private float retreatDistance = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("How much extra fence margin the wolf keeps while retreating")]
    private float retreatFencePadding = 0.6f;

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
    [Tooltip("How long the wolf pauses in place after eating before retreating")]
    private float postEatHoldDuration = 0.8f;

    [SerializeField, Min(0f)]
    [Tooltip("How often the wolf should reevaluate its target")]
    private float retargetInterval = 0.35f;

    [SerializeField, Min(0f)]
    [Tooltip("Minimum time the wolf should visibly stalk before it can attack again")]
    private float stalkDurationMin = 5f;

    [SerializeField, Min(0f)]
    [Tooltip("Maximum time the wolf should visibly stalk before it can attack again")]
    private float stalkDurationMax = 20f;

    [SerializeField]
    [Tooltip("Minimum target score needed before the wolf commits")]
    private float commitThreshold = -0.25f;

    [SerializeField, Min(0f)]
    [Tooltip("How far outside the flock the wolf aims before closing in on a target")]
    private float commitApproachOffset = 1.6f;

    [SerializeField, Min(0f)]
    [Tooltip("How close the wolf gets before it stops using the outside approach")]
    private float commitDirectChaseDistance = 2f;

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

    [Header("State Label")]
    [SerializeField]
    [Tooltip("Local offset for the wolf state label")]
    private Vector3 stateLabelOffset = new(0f, 0f, -0.1f);

    [SerializeField, Min(1)]
    [Tooltip("Font size used by the wolf state label")]
    private int stateLabelFontSize = 48;

    [SerializeField, Min(0.01f)]
    [Tooltip("Character size used by the wolf state label")]
    private float stateLabelCharacterSize = 0.12f;

    [SerializeField]
    [Tooltip("Color used by the wolf state label")]
    private Color stateLabelColor = Color.white;

    private Rigidbody2D rb;
    private Collider2D wolfCollider;
    private TextMesh stateLabel;
    private WolfState state;
    private Vector2 currentVelocity;
    private SheepAgent targetSheep;
    private Vector2 retreatTarget;
    private float cooldownTimer;
    private float postEatHoldTimer;
    private float stateTimer;
    private float nextAttackTime;
    private float stalkDirectionTimer;
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
    private float stalkOrbitDirection = 1f;

    public static IReadOnlyList<Wolf> ActiveWolves => activeWolves;
    public Vector2 CurrentVelocity => currentVelocity;
    public VisualState CurrentVisualState => state switch
    {
        WolfState.Stalk => VisualState.Stalk,
        WolfState.Commit => VisualState.Commit,
        WolfState.Retreat => VisualState.Retreat,
        _ => VisualState.Stalk
    };

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
        EnsureStateLabel();
        EnterStalkState();
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
        stalkDurationMin = Mathf.Max(0f, stalkDurationMin);
        stalkDurationMax = Mathf.Max(stalkDurationMin, stalkDurationMax);

        if (wolfCollider is CircleCollider2D circleCollider)
        {
            circleCollider.radius = collisionRadius;
        }

        if (stateLabel != null)
        {
            ApplyStateLabelSettings();
            UpdateStateLabel();
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
        currentVelocity = Vector2.zero;
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.fixedDeltaTime);
        postEatHoldTimer = Mathf.Max(0f, postEatHoldTimer - Time.fixedDeltaTime);
        retargetTimer -= Time.fixedDeltaTime;
        stalkDirectionTimer -= Time.fixedDeltaTime;
        stateTimer += Time.fixedDeltaTime;
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

        UpdateStateLabel();
    }

    // circles outside the flock until a weak target appears
    private void TickStalk()
    {
        if (!TryGetFlockCenter(out Vector2 flockCenter))
        {
            return;
        }

        UpdateStalkDirection();
        Vector2 stalkPoint = GetStalkPoint(flockCenter);
        bool shouldMove = ShouldMoveDuringStalk();

        if (retargetTimer <= 0f || !IsValidTarget(targetSheep))
        {
            targetSheep = FindBestTarget(flockCenter);
            retargetTimer = retargetInterval;
        }

        if (cooldownTimer <= 0f && IsValidTarget(targetSheep))
        {
            if (stateTimer < nextAttackTime)
            {
                if (shouldMove)
                {
                    MoveTowards(stalkPoint);
                }
                return;
            }

            float targetScore = GetTargetScore(targetSheep, flockCenter);
            lastTargetScore = targetScore;

            if (targetScore >= commitThreshold)
            {
                state = WolfState.Commit;
                stateTimer = 0f;
                TickCommit();
                return;
            }
        }

        if (shouldMove)
        {
            MoveTowards(stalkPoint);
        }
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
            EnterStalkState();
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

        Vector2 flockCenter = rb.position;
        bool hasFlockCenter = flockManager != null && TryGetFlockCenter(out flockCenter);
        lastTargetScore = GetTargetScore(targetSheep, hasFlockCenter ? flockCenter : rb.position);
        MoveTowards(hasFlockCenter ? GetCommitPoint(targetSheep, flockCenter) : targetPosition);
    }

    // backs away from danger or after a successful attack
    private void TickRetreat()
    {
        if (cooldownTimer <= 0f && !lastSheepdogThreatening)
        {
            EnterStalkState();
            return;
        }

        if (postEatHoldTimer > 0f)
        {
            lastHoldingRetreatPosition = true;
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

        EnterStalkState();
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

        float currentDistance = awayFromCenter.magnitude;
        Vector2 radial = awayFromCenter / currentDistance;
        Vector2 tangent = new(-radial.y, radial.x);
        tangent *= stalkOrbitDirection;

        float innerBand = Mathf.Max(0.1f, stalkDistance - stalkBand);
        float outerBand = stalkDistance + stalkBand;

        if (currentDistance < innerBand)
        {
            Vector2 escapeDirection = ((radial * 1.8f) + (tangent * 0.6f)).normalized;
            return flockCenter + (escapeDirection * outerBand);
        }

        if (currentDistance > outerBand)
        {
            Vector2 returnDirection = ((-radial * 0.65f) + (tangent * 1f)).normalized;
            return rb.position + (returnDirection * Mathf.Min(currentDistance - stalkDistance, 2f));
        }

        Vector2 ringPoint = flockCenter + (radial * stalkDistance);
        Vector2 idealPoint = ringPoint + (tangent * stalkLeadDistance);
        return GetFenceSafeTravelPoint(idealPoint, stalkFencePadding);
    }

    // approaches a target from the outside of the flock before closing in to bite
    private Vector2 GetCommitPoint(SheepAgent sheep, Vector2 flockCenter)
    {
        Vector2 sheepPosition = sheep.transform.position;

        if (Vector2.Distance(rb.position, sheepPosition) <= commitDirectChaseDistance)
        {
            return sheepPosition;
        }

        Vector2 outsideDirection = sheepPosition - flockCenter;

        if (outsideDirection.sqrMagnitude <= 0.0001f)
        {
            outsideDirection = sheepPosition - rb.position;
        }

        if (outsideDirection.sqrMagnitude <= 0.0001f)
        {
            return sheepPosition;
        }

        Vector2 approachPoint = sheepPosition + (outsideDirection.normalized * commitApproachOffset);

        if (Vector2.Distance(rb.position, approachPoint) <= arrivalDistance * 4f)
        {
            return sheepPosition;
        }

        return approachPoint;
    }

    // makes the wolf weave around the flock instead of stalking in a straight line
    private void UpdateStalkDirection()
    {
        if (stalkDirectionTimer > 0f)
        {
            return;
        }

        stalkOrbitDirection = Random.value < 0.5f ? -1f : 1f;
        stalkDirectionTimer = stalkDirectionChangeInterval;
    }

    // alternates between repositioning and holding so stalking feels patient
    private bool ShouldMoveDuringStalk()
    {
        float cycleDuration = stalkMoveDuration + stalkHoldDuration;

        if (cycleDuration <= 0f)
        {
            return true;
        }

        if (stalkMoveDuration <= 0f)
        {
            return false;
        }

        return Mathf.Repeat(stateTimer, cycleDuration) < stalkMoveDuration;
    }

    // sends the wolf away from the herd or nearby sheepdogs
    private Vector2 GetRetreatPoint()
    {
        Vector2 preferredDirection = GetSheepdogAvoidanceDirection();

        if (preferredDirection.sqrMagnitude <= 0.0001f && TryGetFlockCenter(out Vector2 flockCenter))
        {
            preferredDirection = rb.position - flockCenter;
        }

        if (preferredDirection.sqrMagnitude <= 0.0001f)
        {
            preferredDirection = Vector2.left;
        }

        return GetBestRetreatPoint(preferredDirection.normalized);
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
        float safeDistance = Mathf.Max(0f, hit.distance - collisionRadius - arrivalDistance - retreatFencePadding);
        return rb.position + (retreatDirection * safeDistance);
    }

    // picks the retreat direction that gives the wolf the most open space away from the flock
    private Vector2 GetBestRetreatPoint(Vector2 preferredDirection)
    {
        float[] angleOffsets = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f, 180f };
        Vector2 bestPoint = rb.position + (preferredDirection * retreatDistance);
        Vector2 bestDirection = preferredDirection;
        float bestScore = float.MinValue;
        bool foundPoint = false;
        RaycastHit2D bestHit = default;
        bool bestAdjusted = false;
        Vector2 flockCenter = rb.position;
        bool hasFlockCenter = TryGetFlockCenter(out flockCenter);

        for (int i = 0; i < angleOffsets.Length; i++)
        {
            Vector2 candidateDirection = Rotate(preferredDirection, angleOffsets[i]).normalized;
            Vector2 candidatePoint = GetReachableRetreatPoint(candidateDirection);
            float travelDistance = Vector2.Distance(rb.position, candidatePoint);

            if (travelDistance <= arrivalDistance)
            {
                continue;
            }

            float alignmentScore = Vector2.Dot(candidateDirection, preferredDirection) * 2f;
            float sheepClearance = GetNearestTargetSheepDistance(candidatePoint);
            float flockDistance = hasFlockCenter ? Vector2.Distance(candidatePoint, flockCenter) : 0f;
            float score = travelDistance * 2.5f + sheepClearance * 1.25f + flockDistance * 0.25f + alignmentScore;

            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestPoint = candidatePoint;
            bestDirection = candidateDirection;
            bestHit = lastRetreatHit;
            bestAdjusted = lastRetreatTargetAdjusted;
            foundPoint = true;
        }

        lastRetreatDirection = bestDirection;
        lastRetreatHit = bestHit;
        lastRetreatTargetAdjusted = bestAdjusted;
        return foundPoint ? bestPoint : GetReachableRetreatPoint(preferredDirection);
    }

    // nudges stalking toward the same general orbit path without hugging fences
    private Vector2 GetFenceSafeTravelPoint(Vector2 desiredPoint, float extraPadding)
    {
        Vector2 preferredDirection = desiredPoint - rb.position;

        if (preferredDirection.sqrMagnitude <= 0.0001f || obstacleLayers.value == 0)
        {
            return desiredPoint;
        }

        float desiredDistance = preferredDirection.magnitude;
        float[] angleOffsets = { 0f, 20f, -20f, 40f, -40f, 70f, -70f };
        Vector2 bestPoint = desiredPoint;
        float bestScore = float.MinValue;

        for (int i = 0; i < angleOffsets.Length; i++)
        {
            Vector2 candidateDirection = Rotate(preferredDirection.normalized, angleOffsets[i]).normalized;
            RaycastHit2D hit = Physics2D.CircleCast(
                rb.position,
                collisionRadius,
                candidateDirection,
                desiredDistance,
                obstacleLayers
            );

            float safeDistance = hit.collider == null
                ? desiredDistance
                : Mathf.Max(0f, hit.distance - collisionRadius - arrivalDistance - extraPadding);

            if (safeDistance <= arrivalDistance)
            {
                continue;
            }

            Vector2 candidatePoint = rb.position + (candidateDirection * safeDistance);
            float score =
                (Vector2.Dot(candidateDirection, preferredDirection.normalized) * 3f) +
                (safeDistance / Mathf.Max(0.01f, desiredDistance));

            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestPoint = candidatePoint;
        }

        return bestPoint;
    }

    // checks how much room a point has before it runs back into the flock
    private float GetNearestTargetSheepDistance(Vector2 point)
    {
        IReadOnlyList<SheepAgent> sheep = flockManager.Sheep;
        float nearestDistance = 0f;
        bool foundSheep = false;

        for (int i = 0; i < sheep.Count; i++)
        {
            SheepAgent candidate = sheep[i];

            if (!IsValidTarget(candidate))
            {
                continue;
            }

            float distance = Vector2.Distance(point, candidate.transform.position);

            if (!foundSheep || distance < nearestDistance)
            {
                nearestDistance = distance;
                foundSheep = true;
            }
        }

        return foundSheep ? nearestDistance : retreatDistance;
    }

    // rotates a 2d direction by the given angle in degrees
    private Vector2 Rotate(Vector2 direction, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        );
    }

    // moves the wolf toward a world position while also respecting sheepdog pressure
    private void MoveTowards(Vector2 targetPosition, bool allowWallSlide = true, float speedMultiplier = 1f)
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
            moveSpeed * speedMultiplier * Time.fixedDeltaTime
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

        currentVelocity = moveDelta / Time.fixedDeltaTime;
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
        postEatHoldTimer = postEatHoldDuration;
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
        stateTimer = 0f;
        retreatTarget = GetRetreatPoint();
        targetSheep = null;
    }

    // resets the wolf to visible stalking instead of instantly reattacking
    private void EnterStalkState()
    {
        state = WolfState.Stalk;
        stateTimer = 0f;
        nextAttackTime = Random.Range(stalkDurationMin, stalkDurationMax);
        stalkDirectionTimer = 0f;
        targetSheep = null;
    }

    // creates a small text label over the wolf for quick state reading
    private void EnsureStateLabel()
    {
        stateLabel = GetComponentInChildren<TextMesh>();

        if (stateLabel == null)
        {
            GameObject labelObject = new("State Label");
            labelObject.transform.SetParent(transform, false);
            stateLabel = labelObject.AddComponent<TextMesh>();
        }

        ApplyStateLabelSettings();
        UpdateStateLabel();
    }

    // keeps the runtime text mesh in sync with the inspector settings
    private void ApplyStateLabelSettings()
    {
        if (stateLabel == null)
        {
            return;
        }

        if (GameFonts.Bold != null)
        {
            stateLabel.font = GameFonts.Bold;
        }

        stateLabel.transform.localPosition = stateLabelOffset;
        stateLabel.anchor = TextAnchor.MiddleCenter;
        stateLabel.alignment = TextAlignment.Center;
        stateLabel.fontSize = stateLabelFontSize;
        stateLabel.characterSize = stateLabelCharacterSize;
        stateLabel.color = stateLabelColor;

        MeshRenderer labelRenderer = stateLabel.GetComponent<MeshRenderer>();

        if (labelRenderer == null)
        {
            return;
        }

        if (stateLabel.font != null)
        {
            labelRenderer.sharedMaterial = stateLabel.font.material;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            labelRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            labelRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
    }

    // shows the current state as one letter on top of the wolf triangle
    private void UpdateStateLabel()
    {
        if (stateLabel == null)
        {
            return;
        }

        stateLabel.text = state switch
        {
            WolfState.Stalk => "S",
            WolfState.Commit => "C",
            WolfState.Retreat => "R",
            _ => "?"
        };
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
            entry.AppendLine($"{indent}    state time: {stateTimer:0.00}");
            entry.AppendLine($"{indent}    attack time: {nextAttackTime:0.00}");
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
