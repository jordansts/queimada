using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ArenaProjectile : MonoBehaviour
{
    private const float LifetimeSeconds = 6f;
    private const float SpawnOwnerCollisionIgnoreSeconds = 0.75f;
    private const float GroundContactNormalThreshold = 0.45f;

    private ArenaCombatant owner;
    private float damage;
    private float knockbackForce;
    private float lifetime;
    private float ownerCollisionIgnoreTimer;
    private bool resolved;
    private bool hasBecomePickup;
    private bool useGravityWhileThrown;
    private Rigidbody projectileRigidbody;
    private SphereCollider sphereCollider;

    private void Awake()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
    }

    public void Initialize(ArenaCombatant owner, Vector3 initialVelocity, float damage, float knockbackForce, bool useGravityWhileThrown)
    {
        this.owner = owner;
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        this.useGravityWhileThrown = useGravityWhileThrown;
        lifetime = LifetimeSeconds;
        ownerCollisionIgnoreTimer = useGravityWhileThrown ? SpawnOwnerCollisionIgnoreSeconds : float.PositiveInfinity;

        if (projectileRigidbody == null)
        {
            projectileRigidbody = GetComponent<Rigidbody>();
        }

        if (sphereCollider == null)
        {
            sphereCollider = GetComponent<SphereCollider>();
        }

        if (owner != null && sphereCollider != null && owner.Colliders != null)
        {
            foreach (Collider ownerCollider in owner.Colliders)
            {
                if (ownerCollider != null)
                {
                    Physics.IgnoreCollision(sphereCollider, ownerCollider, true);
                }
            }
        }

        projectileRigidbody.useGravity = useGravityWhileThrown;
        projectileRigidbody.linearVelocity = initialVelocity;
        projectileRigidbody.angularVelocity = Random.onUnitSphere * 12f;

        if (initialVelocity.sqrMagnitude > 0.0001f)
        {
            transform.forward = initialVelocity.normalized;
        }
    }

    private void FixedUpdate()
    {
        if (resolved || projectileRigidbody == null)
        {
            return;
        }

        if (!hasBecomePickup)
        {
            lifetime -= Time.fixedDeltaTime;
            if (lifetime <= 0f)
            {
                ResolveIntoPickup(transform.position);
                return;
            }

            if (!float.IsPositiveInfinity(ownerCollisionIgnoreTimer))
            {
                ownerCollisionIgnoreTimer -= Time.fixedDeltaTime;
                if (ownerCollisionIgnoreTimer <= 0f)
                {
                    RestoreOwnerCollision();
                }
            }
        }

        Vector3 velocity = projectileRigidbody.linearVelocity;
        if (velocity.sqrMagnitude > 0.04f)
        {
            transform.forward = velocity.normalized;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (resolved)
        {
            return;
        }

        ArenaCombatant target = collision.collider.GetComponentInParent<ArenaCombatant>();
        if (target != null && target != owner)
        {
            Vector3 velocity = projectileRigidbody != null ? projectileRigidbody.linearVelocity : transform.forward;
            Vector3 horizontalDirection = Vector3.ProjectOnPlane(velocity, Vector3.up).normalized;
            if (horizontalDirection.sqrMagnitude < 0.0001f && owner != null)
            {
                horizontalDirection = Vector3.ProjectOnPlane(target.transform.position - owner.transform.position, Vector3.up).normalized;
            }

            Vector3 impulse = (horizontalDirection * 1.15f + Vector3.up * 0.2f).normalized * knockbackForce;
            target.ApplyHit(damage, impulse);

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Vector3 dropPosition = collision.contactCount > 0
                ? contact.point + contact.normal * 0.18f
                : transform.position;

            ResolveIntoPickup(dropPosition);
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (contact.normal.y >= GroundContactNormalThreshold)
            {
                BecomePickup(contact.point + contact.normal * 0.12f);
                return;
            }
        }
    }

    private void RestoreOwnerCollision()
    {
        if (ownerCollisionIgnoreTimer > 0f || owner == null || sphereCollider == null || owner.Colliders == null)
        {
            return;
        }

        foreach (Collider ownerCollider in owner.Colliders)
        {
            if (ownerCollider != null)
            {
                Physics.IgnoreCollision(sphereCollider, ownerCollider, false);
            }
        }

        ownerCollisionIgnoreTimer = float.MinValue;
    }

    private void ResolveIntoPickup(Vector3 position, float pickupDelay = 0.45f)
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        MiniGameManager.Instance?.SpawnArenaBall(position, pickupDelay);
        Destroy(gameObject);
    }

    private void BecomePickup(Vector3 position)
    {
        if (resolved || hasBecomePickup)
        {
            return;
        }

        hasBecomePickup = true;
        ownerCollisionIgnoreTimer = float.MinValue;
        RestoreOwnerCollision();
        useGravityWhileThrown = true;
        if (projectileRigidbody != null)
        {
            projectileRigidbody.useGravity = true;
            projectileRigidbody.linearDamping = 0.06f;
        }

        ArenaBallPickup pickup = GetComponent<ArenaBallPickup>();
        if (pickup == null)
        {
            pickup = gameObject.AddComponent<ArenaBallPickup>();
        }

        pickup.Initialize(position, 0f, false);
        MiniGameManager.Instance?.RegisterLooseArenaBall(pickup);
    }
}
