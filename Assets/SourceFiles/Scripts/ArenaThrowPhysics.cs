using UnityEngine;

public static class ArenaThrowPhysics
{
    public const float ProjectileRadius = 0.34f;
    public const float ReleaseClearance = 0.18f;
    private static readonly RaycastHit[] AimHitBuffer = new RaycastHit[16];

    public static Vector3 ClampTargetToThrowRange(
        Vector3 origin,
        Vector3 target,
        Vector3 fallbackForward,
        float minDistance,
        float maxDistance)
    {
        Vector3 planarOffset = Vector3.ProjectOnPlane(target - origin, Vector3.up);
        Vector3 planarDirection = planarOffset.sqrMagnitude > 0.0001f
            ? planarOffset.normalized
            : Vector3.ProjectOnPlane(fallbackForward, Vector3.up).normalized;

        if (planarDirection.sqrMagnitude < 0.0001f)
        {
            planarDirection = Vector3.forward;
        }

        float clampedDistance = Mathf.Clamp(planarOffset.magnitude, minDistance, maxDistance);
        Vector3 clampedPlanarTarget = origin + planarDirection * clampedDistance;
        return new Vector3(clampedPlanarTarget.x, target.y, clampedPlanarTarget.z);
    }

    public static Vector3 ResolveLaunchDirection(Vector3 preferredDirection, Vector3 fallbackDirection)
    {
        Vector3 direction = preferredDirection.normalized;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = fallbackDirection.normalized;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        return direction;
    }

    public static Vector3 BuildLinearVelocity(Vector3 direction, float launchSpeed)
    {
        return ResolveLaunchDirection(direction, Vector3.forward) * Mathf.Max(0f, launchSpeed);
    }

    public static Vector3 BuildBallisticVelocity(
        Vector3 origin,
        Vector3 target,
        float gravityMagnitude,
        float apexHeight)
    {
        float safeGravity = Mathf.Max(0.01f, gravityMagnitude);
        float apexY = Mathf.Max(apexHeight, origin.y + 0.05f, target.y + 0.05f);

        float riseHeight = Mathf.Max(0.01f, apexY - origin.y);
        float fallHeight = Mathf.Max(0.01f, apexY - target.y);

        float verticalVelocity = Mathf.Sqrt(2f * safeGravity * riseHeight);
        float timeToApex = verticalVelocity / safeGravity;
        float timeToTarget = timeToApex + Mathf.Sqrt(2f * fallHeight / safeGravity);

        Vector3 horizontalOffset = Vector3.ProjectOnPlane(target - origin, Vector3.up);
        Vector3 horizontalVelocity = horizontalOffset / Mathf.Max(0.01f, timeToTarget);
        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    public static Vector3 ResolveAimPoint(
        Camera camera,
        ArenaCombatant owner,
        float maxDistance,
        Vector3 fallbackOrigin,
        Vector3 fallbackDirection)
    {
        Vector3 safeFallbackDirection = ResolveLaunchDirection(fallbackDirection, Vector3.forward);
        if (camera == null)
        {
            return fallbackOrigin + safeFallbackDirection * Mathf.Max(1f, maxDistance);
        }

        Ray aimRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float clampedDistance = Mathf.Max(1f, maxDistance);
        int hitCount = Physics.RaycastNonAlloc(
            aimRay,
            AimHitBuffer,
            clampedDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        RaycastHit bestHit = default;
        bool hasBestHit = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = AimHitBuffer[i];
            AimHitBuffer[i] = default;

            if (hit.collider == null || BelongsToOwner(owner, hit.collider))
            {
                continue;
            }

            if (!hasBestHit || hit.distance < bestHit.distance)
            {
                bestHit = hit;
                hasBestHit = true;
            }
        }

        if (hasBestHit)
        {
            return bestHit.point;
        }

        return aimRay.origin + aimRay.direction * clampedDistance;
    }

    public static Vector3 ResolveReleasePosition(
        Vector3 origin,
        Vector3 launchDirection,
        Vector3 fallbackDirection,
        float projectileRadius,
        float clearance)
    {
        Vector3 releaseDirection = ResolveLaunchDirection(launchDirection, fallbackDirection);
        return origin + releaseDirection * Mathf.Max(0f, projectileRadius + clearance);
    }

    private static bool BelongsToOwner(ArenaCombatant owner, Collider collider)
    {
        if (owner == null || collider == null)
        {
            return false;
        }

        if (collider.transform.IsChildOf(owner.transform))
        {
            return true;
        }

        Collider[] ownerColliders = owner.Colliders;
        if (ownerColliders == null)
        {
            return false;
        }

        for (int i = 0; i < ownerColliders.Length; i++)
        {
            if (ownerColliders[i] == collider)
            {
                return true;
            }
        }

        return false;
    }
}
