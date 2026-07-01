using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ArenaProjectile : MonoBehaviour
{
    [SerializeField] private float lifetimeSeconds = 8f;
    [SerializeField] private float ownerCollisionIgnoreSeconds = 0.3f;
    [SerializeField] private float pickupDelaySeconds = 0.35f;
    [SerializeField] private float settleLinearSpeedThreshold = 1.15f;
    [SerializeField] private float settleAngularSpeedThreshold = 5f;
    [SerializeField] private float settleDelaySeconds = 0.18f;

    private ArenaCombatant owner;
    private float damage;
    private float knockbackForce;
    private float lifetime;
    private float ownerCollisionIgnoreTimer;
    private float settleTimer;
    private bool resolved;
    private bool hasTouchedWorld;
    private bool hasAppliedDamage;
    private Rigidbody projectileRigidbody;
    private SphereCollider sphereCollider;

    private void Awake()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
    }

    public void Initialize(ArenaCombatant owner, Vector3 initialVelocity, float damage, float knockbackForce)
    {
        enabled = true;
        resolved = false;
        hasTouchedWorld = false;
        hasAppliedDamage = false;
        this.owner = owner;
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        lifetime = lifetimeSeconds;
        ownerCollisionIgnoreTimer = ownerCollisionIgnoreSeconds;
        settleTimer = 0f;

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

        sphereCollider.isTrigger = false;
        projectileRigidbody.useGravity = true;
        projectileRigidbody.isKinematic = false;
        projectileRigidbody.detectCollisions = true;
        projectileRigidbody.linearDamping = 0.08f;
        projectileRigidbody.angularDamping = 0.35f;
        projectileRigidbody.linearVelocity = initialVelocity;
        projectileRigidbody.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (resolved || projectileRigidbody == null)
        {
            return;
        }

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

        if (!hasTouchedWorld)
        {
            return;
        }

        float linearSpeed = projectileRigidbody.linearVelocity.magnitude;
        float angularSpeed = projectileRigidbody.angularVelocity.magnitude;
        bool isSettled = linearSpeed <= settleLinearSpeedThreshold && angularSpeed <= settleAngularSpeedThreshold;
        if (!isSettled)
        {
            settleTimer = 0f;
            return;
        }

        settleTimer += Time.fixedDeltaTime;
        if (settleTimer >= settleDelaySeconds || projectileRigidbody.IsSleeping())
        {
            ResolveIntoPickup(transform.position, pickupDelaySeconds);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (resolved)
        {
            return;
        }

        ArenaCombatant target = collision.collider.GetComponentInParent<ArenaCombatant>();
        if (!hasAppliedDamage && target != null && target != owner)
        {
            hasAppliedDamage = true;
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

        hasTouchedWorld = true;
        settleTimer = 0f;
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

    private void ResolveIntoPickup(Vector3 position, float pickupDelay = -1f)
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        RestoreOwnerCollision();

        projectileRigidbody.isKinematic = false;
        projectileRigidbody.linearVelocity = Vector3.zero;
        projectileRigidbody.angularVelocity = Vector3.zero;
        projectileRigidbody.useGravity = false;
        projectileRigidbody.isKinematic = true;
        sphereCollider.isTrigger = true;
        enabled = false;
        MiniGameManager.Instance?.SpawnArenaBall(position, pickupDelay >= 0f ? pickupDelay : pickupDelaySeconds);
        MiniGameManager.Instance?.RecycleArenaBallProjectile(this);
    }
}
