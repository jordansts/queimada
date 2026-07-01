using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArenaPlayerShooter : MonoBehaviour
{
    private const float MinForwardAimDistance = 8f;

    [SerializeField] private float fireCooldown = 0.4f;
    [SerializeField] private float projectileSpeed = 15.5f;
    [SerializeField] private float throwLift = 4.2f;
    [SerializeField] private float damage = 28f;
    [SerializeField] private float knockbackForce = 140f;
    [SerializeField] private float aimRange = 120f;

    private ArenaCombatant owner;
    private ArenaThrowClipPlayer throwClipPlayer;
    private ThirdPersonController thirdPersonController;
    private float cooldownRemaining;
    private Vector3 currentAimPoint;
    private bool hasAimPoint;
    private bool throwQueued;
    private float throwReleaseTimer;
    private Vector3 queuedAimPoint;

    public bool HasAimPoint => hasAimPoint;
    public Vector3 CurrentAimPoint => currentAimPoint;

    public void Initialize(ArenaCombatant owner)
    {
        this.owner = owner;
        throwClipPlayer = GetComponent<ArenaThrowClipPlayer>();
        thirdPersonController = GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        if (owner == null)
        {
            hasAimPoint = false;
            return;
        }

        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
        }

        UpdateAimPoint();
        UpdateQueuedThrow();

        if (WasFirePressedThisFrame() && cooldownRemaining <= 0f && !throwQueued && owner.HasBall)
        {
            Fire();
        }
    }

    private bool WasFirePressedThisFrame()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private void Fire()
    {
        cooldownRemaining = fireCooldown;
        throwClipPlayer?.PlayThrow();
        currentAimPoint = ResolveAimPoint(GetThrowOriginPosition());
        queuedAimPoint = currentAimPoint;
        throwReleaseTimer = throwClipPlayer != null ? throwClipPlayer.ReleaseDelay : 0f;
        throwQueued = true;
    }

    private void UpdateAimPoint()
    {
        Vector3 fallbackOrigin = GetThrowOriginPosition();
        currentAimPoint = ResolveAimPoint(fallbackOrigin);
        hasAimPoint = true;
    }

    private Vector3 ResolveAimPoint(Vector3 fallbackOrigin)
    {
        Vector3 cameraForward = ResolveAimForward();
        Vector3 cameraPosition = ResolveAimOrigin();

        if (cameraForward.sqrMagnitude < 0.0001f)
        {
            return fallbackOrigin + transform.forward * aimRange;
        }

        Ray aimRay = new Ray(cameraPosition, cameraForward);
        Vector3 aimPoint = aimRay.GetPoint(aimRange);

        RaycastHit[] hits = Physics.RaycastAll(aimRay, aimRange, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            ArenaCombatant selfHit = hits[i].collider.GetComponentInParent<ArenaCombatant>();
            if (selfHit == null || selfHit != owner)
            {
                aimPoint = hits[i].point;
                break;
            }
        }

        Vector3 toAimPoint = aimPoint - fallbackOrigin;
        float forwardDistance = Vector3.Dot(cameraForward, toAimPoint);
        if (forwardDistance < MinForwardAimDistance)
        {
            aimPoint = fallbackOrigin + cameraForward * MinForwardAimDistance;
        }

        if (Vector3.Distance(fallbackOrigin, aimPoint) < 0.5f)
        {
            aimPoint = fallbackOrigin + cameraForward * MinForwardAimDistance;
        }

        return aimPoint;
    }

    private Vector3 GetFlatForward()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        return flatForward.sqrMagnitude > 0.0001f ? flatForward : Vector3.forward;
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

        Vector3 spawnPosition = GetThrowOriginPosition();
        Vector3 aimPoint = queuedAimPoint;
        Vector3 cameraForward = ResolveAimForward();
        Vector3 direction = (aimPoint - spawnPosition).normalized;
        if (direction.sqrMagnitude < 0.0001f || Vector3.Dot(direction, cameraForward) <= 0.15f)
        {
            direction = cameraForward;
        }

        spawnPosition += direction * 0.4f;
        Vector3 launchVelocity = ResolveLaunchVelocity(spawnPosition, aimPoint, direction);
        owner.RemoveBall();
        ArenaProjectileFactory.CreateProjectile(
            "PlayerProjectile",
            owner,
            spawnPosition,
            launchVelocity,
            damage,
            knockbackForce);
    }

    private Vector3 ResolveLaunchVelocity(Vector3 origin, Vector3 aimPoint, Vector3 fallbackDirection)
    {
        Vector3 planarOffset = Vector3.ProjectOnPlane(aimPoint - origin, Vector3.up);
        Vector3 planarDirection = planarOffset.sqrMagnitude > 0.0001f
            ? planarOffset.normalized
            : Vector3.ProjectOnPlane(fallbackDirection, Vector3.up).normalized;
        if (planarDirection.sqrMagnitude < 0.0001f)
        {
            planarDirection = transform.forward;
        }

        float planarDistance = planarOffset.magnitude;
        float distanceFactor = Mathf.InverseLerp(MinForwardAimDistance, 22f, planarDistance);
        float horizontalSpeed = Mathf.Lerp(projectileSpeed * 0.92f, projectileSpeed, distanceFactor);
        return planarDirection * horizontalSpeed + Vector3.up * throwLift;
    }

    private Vector3 GetThrowOriginPosition()
    {
        return owner != null && owner.ThrowOrigin != null ? owner.ThrowOrigin.position : transform.position;
    }

    private Vector3 ResolveAimForward()
    {
        if (thirdPersonController != null && thirdPersonController.CinemachineCameraTarget != null)
        {
            Vector3 direction = thirdPersonController.CinemachineCameraTarget.transform.forward;
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }
        }

        if (Camera.main != null)
        {
            Vector3 direction = Camera.main.transform.forward;
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }
        }

        return GetFlatForward();
    }

    private Vector3 ResolveAimOrigin()
    {
        if (thirdPersonController != null && thirdPersonController.CinemachineCameraTarget != null)
        {
            return thirdPersonController.CinemachineCameraTarget.transform.position;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        return GetThrowOriginPosition();
    }
}
