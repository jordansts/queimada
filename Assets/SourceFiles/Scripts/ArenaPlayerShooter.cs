using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArenaPlayerShooter : MonoBehaviour
{
    [SerializeField] private float fireCooldown = 0.4f;
    [SerializeField] private float minThrowDistance = 5f;
    [SerializeField] private float maxThrowDistance = 22f;
    [SerializeField] private float baseArcHeight = 0.08f;
    [SerializeField] private float arcHeightDistanceFactor = 0.005f;
    [SerializeField] private float throwSpeedMultiplier = 1.265625f;
    [SerializeField] private float damage = 28f;
    [SerializeField] private float knockbackForce = 140f;
    [SerializeField] private float aimRange = 120f;
    [SerializeField] private Vector3 fallbackLaunchOffset = new Vector3(0f, 1.1f, 0.28f);
    [SerializeField] private float releaseDetachClearance = 0.02f;

    private ArenaCombatant owner;
    private ArenaThrowClipPlayer throwClipPlayer;
    private ThirdPersonController thirdPersonController;
    private float cooldownRemaining;
    private Vector3 currentAimPoint;
    private bool hasAimPoint;
    private bool throwQueued;
    private float throwReleaseTimer;

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
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        return mousePressed || keyboardPressed;
    }

    private void Fire()
    {
        cooldownRemaining = fireCooldown;
        throwClipPlayer?.PlayThrow();
        throwReleaseTimer = throwClipPlayer != null ? throwClipPlayer.ReleaseDelay : 0f;
        throwQueued = true;
    }

    private void UpdateAimPoint()
    {
        Vector3 origin = ResolveLaunchOriginPosition();
        currentAimPoint = ResolveAimPoint(origin);
        hasAimPoint = true;
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

        Vector3 releaseOriginPosition = ResolveReleaseOriginPosition();
        Vector3 aimPoint = ResolveAimPoint(releaseOriginPosition);
        currentAimPoint = aimPoint;
        Vector3 aimForward = ResolveAimForward();
        Vector3 direction = (aimPoint - releaseOriginPosition).normalized;
        if (direction.sqrMagnitude < 0.0001f || Vector3.Dot(direction, aimForward) <= 0.15f)
        {
            direction = aimForward;
        }

        Vector3 spawnPosition = ArenaThrowPhysics.ResolveReleasePosition(
            releaseOriginPosition,
            direction,
            transform.forward,
            ArenaThrowPhysics.ProjectileRadius,
            releaseDetachClearance);

        Vector3 targetPoint = ArenaThrowPhysics.ClampTargetToThrowRange(
            spawnPosition,
            aimPoint,
            direction,
            minThrowDistance,
            maxThrowDistance);
        Vector3 planarOffset = Vector3.ProjectOnPlane(targetPoint - spawnPosition, Vector3.up);
        float apexHeight = Mathf.Max(
            spawnPosition.y,
            targetPoint.y) + baseArcHeight + planarOffset.magnitude * arcHeightDistanceFactor;
        Vector3 launchVelocity = ArenaThrowPhysics.BuildBallisticVelocity(
            spawnPosition,
            targetPoint,
            Mathf.Abs(Physics.gravity.y),
            apexHeight) * throwSpeedMultiplier;

        ArenaProjectile projectile = ArenaProjectileFactory.CreateProjectile(
            "PlayerProjectile",
            owner,
            spawnPosition,
            launchVelocity,
            damage,
            knockbackForce);

        if (projectile != null)
        {
            owner.RemoveBall();
        }
    }

    private Vector3 ResolveLaunchOriginPosition()
    {
        if (owner != null && owner.ThrowOrigin != null)
        {
            return owner.ThrowOrigin.position;
        }

        return transform.TransformPoint(fallbackLaunchOffset);
    }

    private Vector3 ResolveReleaseOriginPosition()
    {
        if (owner != null)
        {
            return owner.GetBallReleaseOrigin();
        }

        return ResolveLaunchOriginPosition();
    }

    private Vector3 ResolveAimPoint(Vector3 origin)
    {
        return ArenaThrowPhysics.ResolveAimPoint(
            Camera.main,
            owner,
            aimRange,
            origin,
            transform.forward);
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

        return transform.forward;
    }
}
