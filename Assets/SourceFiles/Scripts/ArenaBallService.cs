using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ArenaBallService : MonoBehaviour
{
    private const string BasketBallPrefabPath = "MarpaStudio/Built-In/Prefabs/BasketBall";
    private const string ArenaBallRuntimePrefabPath = "Arena/ArenaBallRuntime";

    public Transform CurrentLooseBallTransform => currentLooseBall != null ? currentLooseBall.transform : null;
    public float VisualScale => visualScale;

    private ArenaBallPickup currentLooseBall;
    private GameObject basketBallPrefab;
    private GameObject runtimeBallPrefab;
    private System.Func<Vector3, Vector3> groundResolver;
    private float visualScale;
    private float visualRadius;
    private float bobbingAmount;
    private float groundClearance;

    public void Configure(
        System.Func<Vector3, Vector3> resolveGroundPosition,
        float ballVisualScale,
        float ballVisualRadius,
        float ballBobbingAmount,
        float ballGroundClearance)
    {
        groundResolver = resolveGroundPosition;
        visualScale = ballVisualScale;
        visualRadius = ballVisualRadius;
        bobbingAmount = ballBobbingAmount;
        groundClearance = ballGroundClearance;
        CacheBallPrefab();
    }

    public void ClaimArenaBall(ArenaBallPickup pickup, ArenaCombatant combatant)
    {
        if (pickup == null || combatant == null)
        {
            return;
        }

        if (currentLooseBall == pickup)
        {
            currentLooseBall = null;
        }

        combatant.GiveBall();
        Destroy(pickup.gameObject);
    }

    public void RegisterLooseArenaBall(ArenaBallPickup pickup)
    {
        if (pickup == null)
        {
            return;
        }

        if (currentLooseBall != null && currentLooseBall != pickup)
        {
            Destroy(currentLooseBall.gameObject);
        }

        currentLooseBall = pickup;
    }

    public void SpawnArenaBall(Vector3 position, float pickupDelay = 0f)
    {
        ClearLooseBall();

        Vector3 groundedPosition = ResolveGroundPosition(position);
        GameObject ballObject = CreateArenaBallInstance("ArenaBallPickup");
        if (ballObject == null)
        {
            return;
        }

        float safeBallHeight = groundedPosition.y + visualRadius + bobbingAmount + groundClearance;
        Vector3 spawnPosition = new Vector3(groundedPosition.x, safeBallHeight, groundedPosition.z);
        ballObject.transform.position = spawnPosition;

        currentLooseBall = ballObject.GetComponent<ArenaBallPickup>();
        if (currentLooseBall == null)
        {
            Debug.LogError("ArenaBallRuntime prefab is missing ArenaBallPickup.");
            Destroy(ballObject);
            return;
        }

        ArenaProjectile projectile = ballObject.GetComponent<ArenaProjectile>();
        if (projectile != null)
        {
            projectile.enabled = false;
        }

        currentLooseBall.enabled = true;
        currentLooseBall.Initialize(spawnPosition, pickupDelay, true, true);
    }

    public void ClearLooseBall()
    {
        if (currentLooseBall == null)
        {
            return;
        }

        Destroy(currentLooseBall.gameObject);
        currentLooseBall = null;
    }

    public GameObject CreateArenaBallVisualInstance(bool includePickupCollider, string objectName)
    {
        GameObject ballObject;
        if (basketBallPrefab != null)
        {
            ballObject = Instantiate(basketBallPrefab);
            ballObject.name = objectName;
            ballObject.transform.localScale = Vector3.one * visualScale;

            foreach (Collider existingCollider in ballObject.GetComponentsInChildren<Collider>(true))
            {
                Destroy(existingCollider);
            }
        }
        else
        {
            ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObject.name = objectName;
            ballObject.transform.localScale = Vector3.one * visualScale;

            Renderer renderer = ballObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.85f, 0.2f);
            }
        }

        if (!includePickupCollider)
        {
            return ballObject;
        }

        SphereCollider pickupCollider = ballObject.GetComponent<SphereCollider>();
        if (pickupCollider == null)
        {
            pickupCollider = ballObject.AddComponent<SphereCollider>();
        }

        pickupCollider.isTrigger = true;
        pickupCollider.radius = 0.5f;
        return ballObject;
    }

    public GameObject CreateArenaBallInstance(string objectName)
    {
        CacheBallPrefab();
        if (runtimeBallPrefab == null)
        {
            Debug.LogError("ArenaBallRuntime prefab could not be loaded. Run Tools/Arena/Rebuild Arena Ball Runtime Prefab.");
            return null;
        }

        GameObject ballObject = Instantiate(runtimeBallPrefab);
        ballObject.name = objectName;
        ballObject.transform.localScale = Vector3.one * visualScale;
        return ballObject;
    }

    private Vector3 ResolveGroundPosition(Vector3 desiredPosition)
    {
        return groundResolver != null ? groundResolver(desiredPosition) : desiredPosition;
    }

    private void CacheBallPrefab()
    {
        if (basketBallPrefab != null)
        {
            if (runtimeBallPrefab != null)
            {
                return;
            }
        }

        basketBallPrefab = Resources.Load<GameObject>(BasketBallPrefabPath);
        runtimeBallPrefab = Resources.Load<GameObject>(ArenaBallRuntimePrefabPath);
        if (basketBallPrefab == null)
        {
            basketBallPrefab = Resources.Load<GameObject>("BasketBall");
        }
#if UNITY_EDITOR
        if (basketBallPrefab == null)
        {
            basketBallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MarpaStudio/Built-In/Prefabs/BasketBall.prefab");
        }

        if (runtimeBallPrefab == null)
        {
            runtimeBallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SourceFiles/Resources/Arena/ArenaBallRuntime.prefab");
        }
#endif
    }
}
