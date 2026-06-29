using UnityEngine;

public static class ArenaBallistics
{
    public static Vector3 CalculateLaunchVelocityForTravelTime(
        Vector3 origin,
        Vector3 target,
        Vector3 gravity,
        float travelTime)
    {
        float safeTravelTime = Mathf.Max(0.01f, travelTime);
        Vector3 displacement = target - origin;
        return displacement / safeTravelTime - 0.5f * gravity * safeTravelTime;
    }

    public static bool TrySolveLaunchVelocityByApex(
        Vector3 origin,
        Vector3 target,
        float gravityMagnitude,
        float extraApexHeight,
        out Vector3 launchVelocity)
    {
        launchVelocity = Vector3.zero;

        Vector3 offset = target - origin;
        Vector3 planarOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
        float verticalOffset = offset.y;
        float apexHeight = Mathf.Max(origin.y, target.y) + Mathf.Max(0.1f, extraApexHeight);
        float heightToApex = apexHeight - origin.y;
        if (heightToApex <= 0.001f)
        {
            return false;
        }

        float upwardVelocity = Mathf.Sqrt(2f * gravityMagnitude * heightToApex);
        float timeToApex = upwardVelocity / gravityMagnitude;

        float fallDistance = apexHeight - target.y;
        if (fallDistance < 0f)
        {
            return false;
        }

        float timeToTarget = timeToApex + Mathf.Sqrt(2f * fallDistance / gravityMagnitude);
        if (timeToTarget <= 0.001f)
        {
            return false;
        }

        Vector3 planarVelocity = planarOffset / timeToTarget;
        launchVelocity = planarVelocity + Vector3.up * upwardVelocity;
        return true;
    }

    public static bool TrySolveLaunchVelocity(
        Vector3 origin,
        Vector3 target,
        float launchSpeed,
        float gravityMagnitude,
        bool preferHighArc,
        out Vector3 launchVelocity)
    {
        launchVelocity = Vector3.zero;

        Vector3 toTarget = target - origin;
        Vector3 planarOffset = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float planarDistance = planarOffset.magnitude;
        float verticalOffset = toTarget.y;

        if (planarDistance <= 0.001f)
        {
            float verticalDirection = verticalOffset >= 0f ? 1f : -1f;
            launchVelocity = Vector3.up * launchSpeed * verticalDirection;
            return true;
        }

        float speedSquared = launchSpeed * launchSpeed;
        float gravityTerm = gravityMagnitude * (gravityMagnitude * planarDistance * planarDistance + 2f * verticalOffset * speedSquared);
        float discriminant = speedSquared * speedSquared - gravityTerm;
        if (discriminant < 0f)
        {
            return false;
        }

        float root = Mathf.Sqrt(discriminant);
        float tanTheta = (speedSquared + (preferHighArc ? root : -root)) / (gravityMagnitude * planarDistance);
        Vector3 planarDirection = planarOffset / planarDistance;
        float cosTheta = 1f / Mathf.Sqrt(1f + tanTheta * tanTheta);
        float sinTheta = tanTheta * cosTheta;

        launchVelocity = planarDirection * (launchSpeed * cosTheta) + Vector3.up * (launchSpeed * sinTheta);
        return true;
    }
}
