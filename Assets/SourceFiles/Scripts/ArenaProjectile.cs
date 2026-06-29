using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ArenaProjectile : MonoBehaviour
{
    private const float HitRadius = 0.18f;
    private const float TargetArrivalDistance = 0.12f;

    private ArenaCombatant owner;
    private float speed;
    private float damage;
    private float knockbackForce;
    private float lifetime;
    private Vector3 flightDirection;
    private Vector3 targetPoint;
    private bool hasTargetPoint;
    private bool resolved;

    public void Initialize(ArenaCombatant owner, Vector3 direction, float speed, float damage, float knockbackForce, Color color, Vector3? targetPoint = null)
    {
        this.owner = owner;
        this.speed = speed;
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        lifetime = 4f;
        flightDirection = direction.normalized;
        if (targetPoint.HasValue)
        {
            this.targetPoint = targetPoint.Value;
            hasTargetPoint = true;
        }
        transform.forward = flightDirection;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 origin = transform.position;
        Vector3 direction = ResolveFlightDirection(origin);

        if (Physics.SphereCast(origin, HitRadius, direction, out RaycastHit hit, step, ~0, QueryTriggerInteraction.Ignore))
        {
            ArenaCombatant target = hit.collider.GetComponentInParent<ArenaCombatant>();
            if (target != null && target != owner)
            {
                Vector3 horizontalDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
                if (horizontalDirection.sqrMagnitude < 0.0001f)
                {
                    horizontalDirection = Vector3.ProjectOnPlane(target.transform.position - owner.transform.position, Vector3.up).normalized;
                }

                Vector3 impulse = (horizontalDirection * 1.2f + Vector3.up * 0.18f).normalized * knockbackForce;
                target.ApplyHit(damage, impulse);
                Vector3 dropDirection = horizontalDirection.sqrMagnitude > 0.0001f ? horizontalDirection : direction;
                ResolveIntoPickup(hit.point + dropDirection * 1.35f);
                return;
            }
        }

        transform.position += direction * step;
        flightDirection = direction;
        transform.forward = direction;

        if (hasTargetPoint && Vector3.Distance(transform.position, targetPoint) <= TargetArrivalDistance)
        {
            ResolveIntoPickup(transform.position);
            return;
        }

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            ResolveIntoPickup(transform.position);
        }
    }

    private Vector3 ResolveFlightDirection(Vector3 origin)
    {
        if (!hasTargetPoint)
        {
            return flightDirection;
        }

        Vector3 toTarget = targetPoint - origin;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return flightDirection;
        }

        return toTarget.normalized;
    }

    private void ResolveIntoPickup(Vector3 position)
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        MiniGameManager.Instance?.SpawnArenaBall(position, 0.45f);
        Destroy(gameObject);
    }
}
