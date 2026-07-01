using UnityEngine;

public static class ArenaThrowPhysics
{
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

    public static bool TrySolveArcVelocity(
        Vector3 origin,
        Vector3 target,
        float gravityMagnitude,
        float arcHeight,
        out Vector3 launchVelocity,
        out float flightTime)
    {
        launchVelocity = Vector3.zero;
        flightTime = 0f;

        if (gravityMagnitude <= 0f)
        {
            return false;
        }

        float apexHeight = Mathf.Max(origin.y, target.y) + Mathf.Max(arcHeight, 0.1f);
        float ascent = apexHeight - origin.y;
        float descent = apexHeight - target.y;
        if (ascent <= 0f || descent < 0f)
        {
            return false;
        }

        float timeUp = Mathf.Sqrt((2f * ascent) / gravityMagnitude);
        float timeDown = Mathf.Sqrt((2f * descent) / gravityMagnitude);
        flightTime = timeUp + timeDown;
        if (flightTime <= 0.0001f)
        {
            return false;
        }

        Vector3 planarVelocity = Vector3.ProjectOnPlane(target - origin, Vector3.up) / flightTime;
        float verticalVelocity = gravityMagnitude * timeUp;
        launchVelocity = planarVelocity + Vector3.up * verticalVelocity;
        return true;
    }

    public static Vector3 ComputeBackspinAngularVelocity(Vector3 linearVelocity, float spinSpeed)
    {
        Vector3 planarDirection = Vector3.ProjectOnPlane(linearVelocity, Vector3.up).normalized;
        if (planarDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 spinAxis = Vector3.Cross(planarDirection, Vector3.up).normalized;
        return spinAxis * spinSpeed;
    }
}
