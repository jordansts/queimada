using UnityEngine;

public static class ArenaProjectileFactory
{
    public static ArenaProjectile CreateProjectile(
        string projectileName,
        ArenaCombatant owner,
        Vector3 position,
        Vector3 direction,
        float speed,
        float damage,
        float knockbackForce,
        Color color,
        Vector3 aimPoint)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = projectileName;
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * 0.35f;

        if (projectileObject.GetComponent<Collider>() is SphereCollider sphereCollider)
        {
            sphereCollider.isTrigger = true;
        }

        ArenaProjectile projectile = projectileObject.AddComponent<ArenaProjectile>();
        projectile.Initialize(owner, direction, speed, damage, knockbackForce, color, aimPoint);
        return projectile;
    }
}
