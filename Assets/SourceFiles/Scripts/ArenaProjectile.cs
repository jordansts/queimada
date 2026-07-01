using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ArenaProjectile : MonoBehaviour
{
    private const float BasketballMass = 0.62f;
    private const float BasketballLinearDamping = 0.015f;
    private const float BasketballAngularDamping = 0.08f;

    [SerializeField] private float lifetimeSeconds = 8f;
    [SerializeField] private float ownerCollisionIgnoreSeconds = 0.3f;
    [SerializeField] private float pickupDelaySeconds = 0.35f;
    [SerializeField] private float settleLinearSpeedThreshold = 0.45f;
    [SerializeField] private float settleAngularSpeedThreshold = 2.2f;
    [SerializeField] private float settleDelaySeconds = 0.55f;
    [SerializeField] private float spawnOverlapIgnoreSeconds = 0.08f;
    [SerializeField] private float spawnOverlapRadiusPadding = 0.02f;

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
    private readonly List<Collider> ignoredSpawnOverlapColliders = new List<Collider>(8);
    private float spawnOverlapIgnoreTimer;
    private static readonly Collider[] SpawnOverlapBuffer = new Collider[16];

    private void Awake()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
    }

    public void Initialize(
        ArenaCombatant owner,
        Vector3 initialVelocity,
        float damage,
        float knockbackForce)
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
        spawnOverlapIgnoreTimer = spawnOverlapIgnoreSeconds;
        settleTimer = 0f;
        ignoredSpawnOverlapColliders.Clear();

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
        projectileRigidbody.mass = BasketballMass;
        projectileRigidbody.position = transform.position;
        projectileRigidbody.rotation = transform.rotation;
        projectileRigidbody.useGravity = true;
        projectileRigidbody.isKinematic = false;
        projectileRigidbody.detectCollisions = true;
        projectileRigidbody.interpolation = RigidbodyInterpolation.None;
        projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        projectileRigidbody.linearDamping = BasketballLinearDamping;
        projectileRigidbody.angularDamping = BasketballAngularDamping;
        projectileRigidbody.maxAngularVelocity = 40f;
        projectileRigidbody.linearVelocity = Vector3.zero;
        projectileRigidbody.angularVelocity = Vector3.zero;
        projectileRigidbody.WakeUp();
        projectileRigidbody.linearVelocity = initialVelocity;

        RegisterSpawnOverlapIgnores();
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

        if (spawnOverlapIgnoreTimer > 0f)
        {
            spawnOverlapIgnoreTimer -= Time.fixedDeltaTime;
            if (spawnOverlapIgnoreTimer <= 0f)
            {
                RestoreSpawnOverlapCollisions();
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

    private void RegisterSpawnOverlapIgnores()
    {
        if (sphereCollider == null || spawnOverlapIgnoreSeconds <= 0f)
        {
            return;
        }

        float radius = sphereCollider.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z));
        radius += Mathf.Max(0f, spawnOverlapRadiusPadding);

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            SpawnOverlapBuffer,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider overlapCollider = SpawnOverlapBuffer[i];
            SpawnOverlapBuffer[i] = null;
            if (overlapCollider == null || overlapCollider == sphereCollider)
            {
                continue;
            }

            if (ignoredSpawnOverlapColliders.Contains(overlapCollider))
            {
                continue;
            }

            Physics.IgnoreCollision(sphereCollider, overlapCollider, true);
            ignoredSpawnOverlapColliders.Add(overlapCollider);
        }
    }

    private void RestoreSpawnOverlapCollisions()
    {
        if (sphereCollider == null)
        {
            ignoredSpawnOverlapColliders.Clear();
            spawnOverlapIgnoreTimer = 0f;
            return;
        }

        for (int i = 0; i < ignoredSpawnOverlapColliders.Count; i++)
        {
            Collider overlapCollider = ignoredSpawnOverlapColliders[i];
            if (overlapCollider != null)
            {
                Physics.IgnoreCollision(sphereCollider, overlapCollider, false);
            }
        }

        ignoredSpawnOverlapColliders.Clear();
        spawnOverlapIgnoreTimer = 0f;
    }

    private void ResolveIntoPickup(Vector3 position, float pickupDelay = -1f)
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        RestoreOwnerCollision();
        RestoreSpawnOverlapCollisions();

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
