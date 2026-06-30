using UnityEngine;

public static class ArenaProjectileFactory
{
    public static ArenaProjectile CreateProjectile(
        string projectileName,
        ArenaCombatant owner,
        Vector3 position,
        Vector3 initialVelocity,
        float damage,
        float knockbackForce,
        bool useGravityWhileThrown = true)
    {
        GameObject projectileObject = MiniGameManager.Instance != null
            ? MiniGameManager.Instance.CreateArenaBallInstance(projectileName)
            : null;

        if (projectileObject == null)
        {
            Debug.LogError("Could not create arena ball projectile instance.");
            return null;
        }

        projectileObject.name = projectileName;
        projectileObject.transform.position = position;

        SphereCollider sphereCollider = projectileObject.GetComponent<SphereCollider>();
        Rigidbody rigidbody = projectileObject.GetComponent<Rigidbody>();
        ArenaProjectile projectile = projectileObject.GetComponent<ArenaProjectile>();
        ArenaBallPickup pickup = projectileObject.GetComponent<ArenaBallPickup>();

        if (sphereCollider == null || rigidbody == null || projectile == null || pickup == null)
        {
            Debug.LogError("ArenaBallRuntime prefab is missing one or more required projectile components.");
            Object.Destroy(projectileObject);
            return null;
        }

        sphereCollider.isTrigger = false;
        rigidbody.linearDamping = useGravityWhileThrown ? 0.06f : 0f;
        rigidbody.angularDamping = 0.03f;
        rigidbody.mass = 0.62f;
        rigidbody.useGravity = useGravityWhileThrown;
        rigidbody.isKinematic = false;

        pickup.enabled = false;
        projectile.Initialize(owner, initialVelocity, damage, knockbackForce, useGravityWhileThrown);
        return projectile;
    }
}
