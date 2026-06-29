using UnityEngine;

public static class ArenaProjectileFactory
{
    private static PhysicsMaterial projectilePhysicMaterial;

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
            ? MiniGameManager.Instance.CreateArenaBallVisualInstance(false, projectileName)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        projectileObject.name = projectileName;
        projectileObject.transform.position = position;

        SphereCollider sphereCollider = projectileObject.GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = projectileObject.AddComponent<SphereCollider>();
        }

        sphereCollider.radius = 0.5f;
        sphereCollider.isTrigger = false;
        sphereCollider.material = GetProjectilePhysicMaterial();

        foreach (Collider collider in projectileObject.GetComponents<Collider>())
        {
            if (collider != sphereCollider)
            {
                Object.Destroy(collider);
            }
        }

        Rigidbody rigidbody = projectileObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = projectileObject.AddComponent<Rigidbody>();
        }

        rigidbody.useGravity = true;
        rigidbody.mass = 0.62f;
        rigidbody.linearDamping = useGravityWhileThrown ? 0.06f : 0f;
        rigidbody.angularDamping = 0.03f;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        ArenaProjectile projectile = projectileObject.AddComponent<ArenaProjectile>();
        projectile.Initialize(owner, initialVelocity, damage, knockbackForce, useGravityWhileThrown);
        return projectile;
    }

    private static PhysicsMaterial GetProjectilePhysicMaterial()
    {
        if (projectilePhysicMaterial != null)
        {
            return projectilePhysicMaterial;
        }

        projectilePhysicMaterial = new PhysicsMaterial("ArenaBallProjectile")
        {
            bounciness = 0.78f,
            dynamicFriction = 0.22f,
            staticFriction = 0.2f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Average
        };

        return projectilePhysicMaterial;
    }
}
