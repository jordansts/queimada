using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ArenaBotController : MonoBehaviour
{
    private enum BotState
    {
        Approach,
        Strafe,
        Retreat,
        Recover,
        Finish
    }

    [SerializeField] private float moveSpeed = 4.8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float preferredRange = 8.5f;
    [SerializeField] private float retreatRange = 6.5f;
    [SerializeField] private float engageRange = 11f;
    [SerializeField] private float personalSpaceRange = 1.75f;
    [SerializeField] private float fireCooldown = 0.75f;
    [SerializeField] private float projectileSpeed = 24f;
    [SerializeField] private float damage = 22f;
    [SerializeField] private float knockbackForce = 120f;
    [SerializeField] private float orbitWeight = 1.15f;
    [SerializeField] private float edgeAvoidanceRadius = 11.5f;
    [SerializeField] private float edgeRecoveryRadius = 12.75f;
    [SerializeField] private float edgeAvoidanceWeight = 2.1f;
    [SerializeField] private float edgePressureRadius = 10.5f;
    [SerializeField] private float fireAngleTolerance = 10f;
    [SerializeField] private float aimPredictionTime = 0.4f;
    [SerializeField] private float aimTargetHeight = 1.1f;
    [SerializeField] private float aimNoise = 0.15f;
    [SerializeField] private float orbitSwapIntervalMin = 1.2f;
    [SerializeField] private float orbitSwapIntervalMax = 2.8f;
    [SerializeField] private float shotRange = 26f;
    [SerializeField] private float visionRange = 32f;
    [SerializeField] private float searchArrivalRadius = 1.35f;
    [SerializeField] private float looseBallMoveSpeedMultiplier = 0.72f;
    [SerializeField] private float looseBallStopRadius = 1.05f;
    [SerializeField] private float looseBallPlayerAvoidanceRadius = 1.7f;
    [SerializeField] private float targetPressureRange = 6.8f;
    [SerializeField] private float animationBlendRate = 10f;

    private ArenaCombatant owner;
    private ArenaThrowClipPlayer throwClipPlayer;
    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity;
    private float cooldownRemaining;
    private float orbitDirection;
    private float orbitSwapTimer;
    private int speedHash;
    private int motionSpeedHash;
    private int moveXHash;
    private int moveYHash;
    private float moveXValue;
    private float moveYValue;
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private BotState currentState;
    private Vector3 currentMoveDirection;
    private Vector3 currentAimPoint;
    private Vector3 lastKnownTargetPosition;
    private bool hasLastKnownTargetPosition;
    private bool hasLineOfSight;
    private bool throwQueued;
    private float throwReleaseTimer;
    private Vector3 queuedSpawnPosition;
    private Vector3 queuedDirection;
    private Vector3 queuedAimPoint;

    public void Initialize(ArenaCombatant owner)
    {
        this.owner = owner;
        throwClipPlayer = GetComponent<ArenaThrowClipPlayer>();
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        orbitDirection = Random.value > 0.5f ? 1f : -1f;
        orbitSwapTimer = Random.Range(orbitSwapIntervalMin, orbitSwapIntervalMax);
        speedHash = Animator.StringToHash("Speed");
        motionSpeedHash = Animator.StringToHash("MotionSpeed");
        moveXHash = Animator.StringToHash("MoveX");
        moveYHash = Animator.StringToHash("MoveY");
    }

    private void Update()
    {
        if (owner == null || owner.Opponent == null || controller == null)
        {
            return;
        }

        ArenaCombatant target = owner.Opponent;
        UpdateQueuedThrow();

        if (!owner.HasBall)
        {
            HandleLooseBallState(target);
            return;
        }

        HandleArmedState(target);
    }

    private void HandleLooseBallState(ArenaCombatant target)
    {
        Transform looseBall = MiniGameManager.Instance != null ? MiniGameManager.Instance.CurrentLooseBallTransform : null;
        UpdateBallChase(target, looseBall);
        TickCooldown();
    }

    private void HandleArmedState(ArenaCombatant target)
    {
        Vector3 toTarget = target.transform.position - transform.position;
        Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
        float distance = flatToTarget.magnitude;
        UpdateTargetVelocity(target);
        UpdatePerception(target);
        UpdateOrbitDirection(distance);
        UpdateDecision(target, flatToTarget, distance);
        RotateTowardAimPoint();
        ApplyMovement(currentMoveDirection, moveSpeed);
        TickCooldown();

        if (cooldownRemaining <= 0f && !throwQueued && CanFireAt(target))
        {
            FireAt(target);
            cooldownRemaining = fireCooldown * Random.Range(0.85f, 1.1f);
        }
    }

    private void FireAt(ArenaCombatant target)
    {
        throwClipPlayer?.PlayThrow();

        Transform muzzle = owner.ThrowOrigin != null ? owner.ThrowOrigin : transform;
        Vector3 aimPoint = ResolveAimPoint(target);
        Vector3 direction = (aimPoint - muzzle.position).normalized;
        queuedSpawnPosition = muzzle.position + direction * 0.4f;
        queuedDirection = direction;
        queuedAimPoint = aimPoint;
        throwReleaseTimer = throwClipPlayer != null ? throwClipPlayer.ReleaseDelay : 0f;
        throwQueued = true;
    }

    private void UpdateTargetVelocity(ArenaCombatant target)
    {
        if (lastTargetPosition == Vector3.zero)
        {
            lastTargetPosition = target.transform.position;
            targetVelocity = Vector3.zero;
            return;
        }

        targetVelocity = (target.transform.position - lastTargetPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastTargetPosition = target.transform.position;
    }

    private void UpdateOrbitDirection(float distance)
    {
        orbitSwapTimer -= Time.deltaTime;
        if (orbitSwapTimer > 0f)
        {
            return;
        }

        orbitSwapTimer = Random.Range(orbitSwapIntervalMin, orbitSwapIntervalMax);
        orbitDirection = distance < retreatRange || Random.value > 0.5f ? -orbitDirection : orbitDirection;
    }

    private void UpdateDecision(ArenaCombatant target, Vector3 flatToTarget, float distance)
    {
        Vector3 pursuitPoint = hasLineOfSight
            ? target.transform.position
            : hasLastKnownTargetPosition
                ? lastKnownTargetPosition
                : target.transform.position;
        Vector3 pursuitOffset = pursuitPoint - transform.position;
        pursuitOffset.y = 0f;
        Vector3 desiredDirection = pursuitOffset.sqrMagnitude > 0.0001f ? pursuitOffset.normalized : transform.forward;
        Vector3 selfOffset = Vector3.ProjectOnPlane(transform.position, Vector3.up);
        Vector3 targetOffset = Vector3.ProjectOnPlane(target.transform.position, Vector3.up);
        float selfDistanceFromCenter = selfOffset.magnitude;
        float targetDistanceFromCenter = targetOffset.magnitude;
        float distanceToSearchPoint = pursuitOffset.magnitude;

        bool needRecovery = selfDistanceFromCenter >= edgeRecoveryRadius;
        bool shouldFinish = hasLineOfSight && targetDistanceFromCenter >= edgePressureRadius && selfDistanceFromCenter < edgeAvoidanceRadius;
        bool shouldSearch = !hasLineOfSight && hasLastKnownTargetPosition && distanceToSearchPoint > searchArrivalRadius;

        if (needRecovery)
        {
            currentState = BotState.Recover;
        }
        else if (shouldSearch)
        {
            currentState = BotState.Approach;
        }
        else if (distance < personalSpaceRange)
        {
            currentState = BotState.Recover;
        }
        else if (distance < retreatRange)
        {
            currentState = BotState.Retreat;
        }
        else if (shouldFinish)
        {
            currentState = BotState.Finish;
        }
        else if (distance > engageRange)
        {
            currentState = BotState.Approach;
        }
        else
        {
            currentState = BotState.Strafe;
        }

        Vector3 moveDirection = BuildMoveDirection(currentState, desiredDirection, selfOffset, targetOffset);
        currentMoveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
        currentAimPoint = ResolveAimPoint(target);
    }

    private bool CanFireAt(ArenaCombatant target)
    {
        if (!owner.HasBall)
        {
            return false;
        }

        Transform muzzle = owner.ThrowOrigin != null ? owner.ThrowOrigin : transform;
        Vector3 aimPoint = currentAimPoint == Vector3.zero ? ResolveAimPoint(target) : currentAimPoint;
        Vector3 shotDirection = (aimPoint - muzzle.position).normalized;

        if (Vector3.Angle(transform.forward, shotDirection) > fireAngleTolerance)
        {
            return false;
        }

        if (Physics.Raycast(muzzle.position, shotDirection, out RaycastHit hit, shotRange, ~0, QueryTriggerInteraction.Ignore))
        {
            ArenaCombatant hitCombatant = hit.collider.GetComponentInParent<ArenaCombatant>();
            if (hitCombatant != null && hitCombatant != target)
            {
                return false;
            }

            if (hitCombatant == target)
            {
                return true;
            }
        }

        Vector3 flatToTarget = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
        float distance = flatToTarget.magnitude;
        return hasLineOfSight && (currentState == BotState.Finish || distance <= engageRange);
    }

    private Vector3 BuildMoveDirection(BotState state, Vector3 desiredDirection, Vector3 selfOffset, Vector3 targetOffset)
    {
        Vector3 strafeDirection = Vector3.Cross(Vector3.up, desiredDirection).normalized * orbitDirection;
        Vector3 edgeRecovery = selfOffset.sqrMagnitude > 0.0001f ? -selfOffset.normalized * edgeAvoidanceWeight : Vector3.zero;
        Vector3 pressureDirection = targetOffset.sqrMagnitude > 0.0001f ? targetOffset.normalized : desiredDirection;
        Vector3 targetSeparation = -desiredDirection;

        switch (state)
        {
            case BotState.Recover:
                return edgeRecovery - strafeDirection * 0.45f + targetSeparation * 0.9f;
            case BotState.Retreat:
                return -desiredDirection * 1.05f + strafeDirection * 0.55f + edgeRecovery * 0.6f;
            case BotState.Finish:
                return pressureDirection * 1.1f + strafeDirection * 0.35f + edgeRecovery * 0.25f;
            case BotState.Approach:
                return desiredDirection * 1.15f + strafeDirection * 0.25f + edgeRecovery * 0.2f;
            default:
                return strafeDirection * orbitWeight + desiredDirection * 0.45f + edgeRecovery * 0.3f;
        }
    }

    private Vector3 ResolveAimPoint(ArenaCombatant target)
    {
        Transform muzzle = owner.ThrowOrigin != null ? owner.ThrowOrigin : transform;
        Vector3 targetCenter = target.transform.position + Vector3.up * aimTargetHeight;
        float distance = Vector3.Distance(muzzle.position, targetCenter);
        float travelTime = distance / Mathf.Max(projectileSpeed, 0.1f);
        float predictionTime = Mathf.Min(aimPredictionTime + travelTime * 0.35f, 0.9f);
        Vector3 predictedPoint = targetCenter + targetVelocity * predictionTime;

        if (currentState != BotState.Finish)
        {
            predictedPoint += new Vector3(
                Random.Range(-aimNoise, aimNoise),
                Random.Range(-aimNoise * 0.25f, aimNoise * 0.25f),
                Random.Range(-aimNoise, aimNoise));
        }

        return predictedPoint;
    }

    private void RotateTowardAimPoint()
    {
        Vector3 aimDirection = Vector3.ProjectOnPlane(currentAimPoint - transform.position, Vector3.up);
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            aimDirection = transform.forward;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdatePerception(ArenaCombatant target)
    {
        Transform muzzle = owner.ThrowOrigin != null ? owner.ThrowOrigin : transform;
        Vector3 targetCenter = target.transform.position + Vector3.up * aimTargetHeight;
        Vector3 toTarget = targetCenter - muzzle.position;
        float distance = toTarget.magnitude;

        if (distance > visionRange || toTarget.sqrMagnitude < 0.0001f)
        {
            hasLineOfSight = false;
            return;
        }

        Vector3 direction = toTarget / distance;
        if (Physics.Raycast(muzzle.position, direction, out RaycastHit hit, visionRange, ~0, QueryTriggerInteraction.Ignore))
        {
            ArenaCombatant seenCombatant = hit.collider.GetComponentInParent<ArenaCombatant>();
            hasLineOfSight = seenCombatant == target;
        }
        else
        {
            hasLineOfSight = true;
        }

        if (hasLineOfSight)
        {
            lastKnownTargetPosition = target.transform.position;
            hasLastKnownTargetPosition = true;
        }
    }

    private void UpdateQueuedThrow()
    {
        if (!throwQueued)
        {
            return;
        }

        throwReleaseTimer -= Time.deltaTime;
        if (throwReleaseTimer > 0f)
        {
            return;
        }

        throwQueued = false;
        if (owner == null || !owner.HasBall)
        {
            return;
        }

        owner.RemoveBall();
        ArenaProjectileFactory.CreateProjectile(
            "BotProjectile",
            owner,
            queuedSpawnPosition,
            queuedDirection * projectileSpeed,
            damage,
            knockbackForce);
    }

    private void UpdateBallChase(ArenaCombatant target, Transform looseBall)
    {
        Vector3 goalPosition = looseBall != null ? looseBall.position : target.transform.position;
        Vector3 flatToGoal = Vector3.ProjectOnPlane(goalPosition - transform.position, Vector3.up);
        Vector3 selfOffset = Vector3.ProjectOnPlane(transform.position, Vector3.up);
        Vector3 moveDirection = Vector3.zero;

        if (looseBall != null)
        {
            float distanceToBall = flatToGoal.magnitude;
            if (distanceToBall > looseBallStopRadius)
            {
                moveDirection += flatToGoal.normalized;
            }

            Vector3 flatFromTarget = Vector3.ProjectOnPlane(transform.position - target.transform.position, Vector3.up);
            float distanceToTarget = flatFromTarget.magnitude;
            if (distanceToTarget < looseBallPlayerAvoidanceRadius && flatFromTarget.sqrMagnitude > 0.0001f)
            {
                moveDirection += flatFromTarget.normalized * 1.35f;
            }
        }
        else
        {
            Vector3 flatToTarget = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
            float distanceToTarget = flatToTarget.magnitude;
            Vector3 desiredDirection = flatToTarget.sqrMagnitude > 0.0001f ? flatToTarget.normalized : transform.forward;
            Vector3 strafeDirection = Vector3.Cross(Vector3.up, desiredDirection).normalized * orbitDirection;

            if (distanceToTarget > targetPressureRange + 0.45f)
            {
                moveDirection += desiredDirection;
            }
            else if (distanceToTarget < targetPressureRange - 0.45f)
            {
                moveDirection -= desiredDirection;
            }

            moveDirection += strafeDirection * 0.85f;
        }

        if (selfOffset.magnitude >= edgeRecoveryRadius)
        {
            moveDirection += -selfOffset.normalized * edgeAvoidanceWeight;
        }

        currentMoveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
        currentAimPoint = looseBall != null ? looseBall.position : target.transform.position + Vector3.up * aimTargetHeight;
        RotateTowardAimPoint();
        ApplyMovement(currentMoveDirection, moveSpeed * looseBallMoveSpeedMultiplier);
    }

    private void ApplyMovement(Vector3 moveDirection, float moveSpeedMultiplier)
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 motion = moveDirection * moveSpeedMultiplier;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);

        if (animator != null)
        {
            Vector3 planarVelocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);
            float normalizedMotion = moveSpeedMultiplier > 0.001f ? Mathf.Clamp01(planarVelocity.magnitude / moveSpeedMultiplier) : 0f;
            Vector3 localMoveDirection = transform.InverseTransformDirection(Vector3.ProjectOnPlane(moveDirection, Vector3.up));
            float moveTier = normalizedMotion > 0.85f ? 2f : 1f;
            float targetMoveX = Mathf.Clamp(localMoveDirection.x, -1f, 1f) * moveTier;
            float targetMoveY = Mathf.Clamp(localMoveDirection.z, -1f, 1f) * moveTier;

            moveXValue = Mathf.Lerp(moveXValue, targetMoveX, Time.deltaTime * animationBlendRate);
            moveYValue = Mathf.Lerp(moveYValue, targetMoveY, Time.deltaTime * animationBlendRate);

            animator.SetFloat(speedHash, planarVelocity.magnitude);
            animator.SetFloat(motionSpeedHash, normalizedMotion);
            animator.SetFloat(moveXHash, moveXValue);
            animator.SetFloat(moveYHash, moveYValue);
        }
    }

    private void TickCooldown()
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
        }
    }
}
