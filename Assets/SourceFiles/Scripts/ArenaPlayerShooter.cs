using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ArenaPlayerShooter : MonoBehaviour
{
    private const float MinForwardAimDistance = 8f;

    [SerializeField] private float fireCooldown = 0.4f;
    [SerializeField] private float projectileSpeed = 28f;
    [SerializeField] private float damage = 28f;
    [SerializeField] private float knockbackForce = 140f;
    [SerializeField] private float aimRange = 120f;
    [SerializeField] private float aimHeight = 1.1f;

    private ArenaCombatant owner;
    private ArenaThrowClipPlayer throwClipPlayer;
    private float cooldownRemaining;
    private Vector3 currentAimPoint;
    private bool hasAimPoint;
    private bool throwQueued;
    private float throwReleaseTimer;
    private Vector3 queuedSpawnPosition;
    private Vector3 queuedDirection;
    private Vector3 queuedAimPoint;

    public bool HasAimPoint => hasAimPoint;
    public Vector3 CurrentAimPoint => currentAimPoint;

    public void Initialize(ArenaCombatant owner)
    {
        this.owner = owner;
        throwClipPlayer = GetComponent<ArenaThrowClipPlayer>();
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
#if ENABLE_INPUT_SYSTEM
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return mousePressed;
#else
    return Input.GetMouseButtonDown(0);
#endif
    }

    private void Fire()
    {
        cooldownRemaining = fireCooldown;
        throwClipPlayer?.PlayThrow();

        Transform muzzle = owner.WeaponMuzzle != null ? owner.WeaponMuzzle : transform;
        Vector3 spawnPosition = muzzle.position;
        Vector3 aimPoint = ResolveAimPoint(spawnPosition);
        Vector3 direction = (aimPoint - spawnPosition).normalized;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = GetFlatForward();
            aimPoint = spawnPosition + direction * aimRange;
        }

        spawnPosition += direction * 0.4f;
        queuedSpawnPosition = spawnPosition;
        queuedDirection = direction;
        queuedAimPoint = aimPoint;
        throwReleaseTimer = throwClipPlayer != null ? throwClipPlayer.ReleaseDelay : 0f;
        throwQueued = true;
    }

    private void SpawnProjectile(Vector3 position, Vector3 direction, Vector3 aimPoint, Color color)
    {
        ArenaProjectileFactory.CreateProjectile(
            "PlayerProjectile",
            owner,
            position,
            direction,
            projectileSpeed,
            damage,
            knockbackForce,
            color,
            aimPoint);
    }

    private void UpdateAimPoint()
    {
        Vector3 fallbackOrigin = owner != null && owner.WeaponMuzzle != null ? owner.WeaponMuzzle.position : transform.position;
        currentAimPoint = ResolveAimPoint(fallbackOrigin);
        hasAimPoint = true;
    }

    private Vector3 ResolveAimPoint(Vector3 fallbackOrigin)
    {
        if (Camera.main == null)
        {
            return fallbackOrigin + transform.forward * aimRange;
        }

        Camera mainCamera = Camera.main;
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward.normalized;
        Ray aimRay = new Ray(cameraPosition, cameraForward);
        Vector3 aimPoint = aimRay.GetPoint(aimRange);

        if (Physics.Raycast(aimRay, out RaycastHit hit, aimRange, ~0, QueryTriggerInteraction.Ignore))
        {
            aimPoint = hit.point;
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

        owner.RemoveBall();
        SpawnProjectile(queuedSpawnPosition, queuedDirection, queuedAimPoint, new Color(0.25f, 0.9f, 1f));
    }
}
