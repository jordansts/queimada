using UnityEngine;

public class ArenaBallService : MonoBehaviour
{
    private const bool LooseBallUsesFloatingMotion = false;
    private const bool LooseBallUsesTriggerCollider = false;

    public Transform CurrentLooseBallTransform => currentLooseBall != null ? currentLooseBall.transform : null;

    [SerializeField] private ArenaBallPickup sceneLooseBall;
    [SerializeField] private ArenaProjectile sceneProjectileBall;

    private ArenaBallPickup currentLooseBall;
    private System.Func<Vector3, Vector3> groundResolver;
    private float visualRadius;
    private float bobbingAmount;
    private float groundClearance;

    public void Configure(
        System.Func<Vector3, Vector3> resolveGroundPosition,
        float ballVisualRadius,
        float ballBobbingAmount,
        float ballGroundClearance)
    {
        groundResolver = resolveGroundPosition;
        visualRadius = ballVisualRadius;
        bobbingAmount = ballBobbingAmount;
        groundClearance = ballGroundClearance;
        ValidateSceneReferences();
    }

    public void ClaimArenaBall(ArenaBallPickup pickup, ArenaCombatant combatant)
    {
        if (pickup == null || combatant == null)
        {
            return;
        }

        if (pickup != sceneLooseBall)
        {
            Debug.LogError("ArenaBallService expected the scene loose ball instance only.", pickup);
            return;
        }

        if (currentLooseBall == sceneLooseBall)
        {
            currentLooseBall = null;
        }

        combatant.GiveBall();
        ResetSceneLooseBall();
    }

    public void RegisterLooseArenaBall(ArenaBallPickup pickup)
    {
        if (pickup == null)
        {
            return;
        }

        if (pickup != sceneLooseBall)
        {
            Debug.LogError("ArenaBallService expected the scene loose ball instance only.", pickup);
            return;
        }

        currentLooseBall = pickup;
    }

    public void PrepareLooseArenaBall(
        ArenaBallPickup pickup,
        Vector3 position,
        float pickupDelay,
        bool useFloatingMotion,
        bool useTriggerCollider)
    {
        if (pickup == null)
        {
            return;
        }

        Vector3 groundedPosition = ResolveGroundPosition(position);
        float safeHeight = groundedPosition.y + visualRadius + groundClearance;
        if (useFloatingMotion)
        {
            safeHeight += bobbingAmount;
        }

        Vector3 safePosition = new Vector3(groundedPosition.x, safeHeight, groundedPosition.z);
        pickup.transform.position = safePosition;
        pickup.enabled = true;
        pickup.Initialize(safePosition, pickupDelay, useFloatingMotion, useTriggerCollider);
        RegisterLooseArenaBall(pickup);
    }

    public void SpawnArenaBall(Vector3 position, float pickupDelay = 0f)
    {
        ClearLooseBall();

        ArenaBallPickup looseBall = GetLooseBallInstance();
        if (looseBall == null)
        {
            return;
        }

        currentLooseBall = looseBall;
        PrepareLooseArenaBall(
            currentLooseBall,
            position,
            pickupDelay,
            LooseBallUsesFloatingMotion,
            LooseBallUsesTriggerCollider);
    }

    public void ClearLooseBall()
    {
        if (currentLooseBall == null)
        {
            return;
        }

        if (currentLooseBall != sceneLooseBall)
        {
            Debug.LogError("ArenaBallService expected the scene loose ball instance only.", currentLooseBall);
        }

        ResetSceneLooseBall();
        currentLooseBall = null;
    }

    public ArenaProjectile ActivateSceneProjectile(string objectName, Vector3 position)
    {
        ValidateSceneReferences();
        if (sceneProjectileBall == null)
        {
            Debug.LogError("ArenaBallService could not find ArenaBallProjectile in the scene.");
            return null;
        }

        Rigidbody rigidbody = sceneProjectileBall.GetComponent<Rigidbody>();
        SphereCollider sphereCollider = sceneProjectileBall.GetComponent<SphereCollider>();
        if (rigidbody != null)
        {
            rigidbody.useGravity = false;
            rigidbody.detectCollisions = false;
            rigidbody.isKinematic = false;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.position = position;
            rigidbody.rotation = Quaternion.identity;
            rigidbody.isKinematic = true;
            rigidbody.Sleep();
        }

        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = true;
        }

        sceneProjectileBall.gameObject.name = objectName;
        sceneProjectileBall.transform.SetPositionAndRotation(position, Quaternion.identity);
        sceneProjectileBall.gameObject.SetActive(true);
        sceneProjectileBall.enabled = true;
        return sceneProjectileBall;
    }

    public void RecycleProjectile(ArenaProjectile projectile)
    {
        if (projectile == null)
        {
            return;
        }

        if (projectile != sceneProjectileBall)
        {
            Debug.LogError("ArenaBallService expected the scene projectile instance only.", projectile);
            return;
        }

        ResetSceneProjectileBall();
    }

    private Vector3 ResolveGroundPosition(Vector3 desiredPosition)
    {
        return groundResolver != null ? groundResolver(desiredPosition) : desiredPosition;
    }

    private void ValidateSceneReferences()
    {
        if (sceneLooseBall == null)
        {
            Debug.LogError("ArenaBallService requires an ArenaBallPickup object in the scene.");
        }

        if (sceneProjectileBall == null)
        {
            Debug.LogError("ArenaBallService requires an ArenaBallProjectile object in the scene.");
        }
    }

    private ArenaBallPickup GetLooseBallInstance()
    {
        ValidateSceneReferences();
        if (sceneLooseBall == null)
        {
            return null;
        }

        sceneLooseBall.gameObject.SetActive(true);
        return sceneLooseBall;
    }

    private void ResetSceneLooseBall()
    {
        if (sceneLooseBall == null)
        {
            return;
        }

        Rigidbody rigidbody = sceneLooseBall.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = false;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
        }

        sceneLooseBall.gameObject.SetActive(false);
    }

    private void ResetSceneProjectileBall()
    {
        if (sceneProjectileBall == null)
        {
            return;
        }

        Rigidbody rigidbody = sceneProjectileBall.GetComponent<Rigidbody>();
        SphereCollider sphereCollider = sceneProjectileBall.GetComponent<SphereCollider>();
        if (rigidbody != null)
        {
            rigidbody.useGravity = false;
            rigidbody.detectCollisions = false;
            rigidbody.isKinematic = false;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.position = sceneProjectileBall.transform.position;
            rigidbody.rotation = sceneProjectileBall.transform.rotation;
            rigidbody.isKinematic = true;
            rigidbody.Sleep();
        }

        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = true;
        }

        sceneProjectileBall.enabled = false;
        sceneProjectileBall.gameObject.SetActive(false);
    }
}
