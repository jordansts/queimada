using UnityEngine;

public static class ArenaProjectileFactory
{
    public static ArenaProjectile CreateProjectile(
        string projectileName,
        ArenaCombatant owner,
        Vector3 position,
        Vector3 initialVelocity,
        float damage,
        float knockbackForce)
    {
        ArenaProjectile projectile = MiniGameManager.Instance != null
            ? MiniGameManager.Instance.ActivateArenaBallProjectile(projectileName, position)
            : null;

        if (projectile == null)
        {
            Debug.LogError("Could not create arena ball projectile instance.");
            return null;
        }

        SphereCollider sphereCollider = projectile.GetComponent<SphereCollider>();
        Rigidbody rigidbody = projectile.GetComponent<Rigidbody>();

        if (sphereCollider == null || rigidbody == null || projectile == null)
        {
            Debug.LogError("ArenaBallProjectile scene object is missing one or more required projectile components.", projectile);
            MiniGameManager.Instance?.RecycleArenaBallProjectile(projectile);
            return null;
        }

        sphereCollider.isTrigger = false;
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        rigidbody.detectCollisions = true;

        projectile.Initialize(owner, initialVelocity, damage, knockbackForce);
        return projectile;
    }
}
