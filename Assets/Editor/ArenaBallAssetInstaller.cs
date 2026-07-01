#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ArenaBallAssetInstaller
{
    private const string BallVisualPrefabPath = "Assets/MarpaStudio/Built-In/Prefabs/BasketBall.prefab";
    private const string PickupBallPrefabPath = "Assets/SourceFiles/Resources/Arena/ArenaBallRuntime.prefab";
    private const string ProjectileBallPrefabPath = "Assets/SourceFiles/Resources/Arena/ArenaBallProjectile.prefab";
    private const string ProjectileMaterialPath = "Assets/SourceFiles/Materials/ArenaBallProjectile.asset";

    [MenuItem("Tools/Arena/Rebuild Arena Ball Prefabs")]
    public static void Rebuild()
    {
        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BallVisualPrefabPath);
        if (visualPrefab == null)
        {
            throw new System.InvalidOperationException($"Could not load ball visual prefab at '{BallVisualPrefabPath}'.");
        }

        EnsureFolder("Assets/SourceFiles/Resources");
        EnsureFolder("Assets/SourceFiles/Resources/Arena");
        EnsureFolder("Assets/SourceFiles/Materials");

        PhysicsMaterial projectileMaterial = LoadOrCreateProjectileMaterial();
        BuildPickupPrefab(visualPrefab, projectileMaterial);
        BuildProjectilePrefab(visualPrefab, projectileMaterial);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildPickupPrefab(GameObject visualPrefab, PhysicsMaterial projectileMaterial)
    {
        GameObject root = new GameObject("ArenaBallRuntime");

        SphereCollider sphereCollider = root.AddComponent<SphereCollider>();
        sphereCollider.radius = 0.5f;
        sphereCollider.isTrigger = true;
        sphereCollider.material = projectileMaterial;

        Rigidbody rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.mass = 0.62f;
        rigidbody.linearDamping = 0.06f;
        rigidbody.angularDamping = 0.03f;
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        root.AddComponent<ArenaBallPickup>();
        AttachVisual(root, visualPrefab);
        PrefabUtility.SaveAsPrefabAsset(root, PickupBallPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildProjectilePrefab(GameObject visualPrefab, PhysicsMaterial projectileMaterial)
    {
        GameObject root = new GameObject("ArenaBallProjectile");

        SphereCollider sphereCollider = root.AddComponent<SphereCollider>();
        sphereCollider.radius = 0.5f;
        sphereCollider.isTrigger = false;
        sphereCollider.material = projectileMaterial;

        Rigidbody rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.mass = 0.62f;
        rigidbody.linearDamping = 0.015f;
        rigidbody.angularDamping = 0.08f;
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        root.AddComponent<ArenaProjectile>();
        AttachVisual(root, visualPrefab);
        PrefabUtility.SaveAsPrefabAsset(root, ProjectileBallPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void AttachVisual(GameObject root, GameObject visualPrefab)
    {
        GameObject visualInstance = PrefabUtility.InstantiatePrefab(visualPrefab) as GameObject;
        if (visualInstance == null)
        {
            Object.DestroyImmediate(root);
            throw new System.InvalidOperationException("Could not instantiate the official BasketBall prefab.");
        }

        visualInstance.name = "Visual";
        visualInstance.transform.SetParent(root.transform, false);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        foreach (Collider collider in visualInstance.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static PhysicsMaterial LoadOrCreateProjectileMaterial()
    {
        PhysicsMaterial existingMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(ProjectileMaterialPath);
        if (existingMaterial != null)
        {
            existingMaterial.bounciness = 0.78f;
            existingMaterial.dynamicFriction = 0.28f;
            existingMaterial.staticFriction = 0.32f;
            existingMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
            existingMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(existingMaterial);
            return existingMaterial;
        }

        PhysicsMaterial projectileMaterial = new PhysicsMaterial("ArenaBallProjectile")
        {
            bounciness = 0.78f,
            dynamicFriction = 0.28f,
            staticFriction = 0.32f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        AssetDatabase.CreateAsset(projectileMaterial, ProjectileMaterialPath);
        return projectileMaterial;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        int slashIndex = assetPath.LastIndexOf('/');
        if (slashIndex <= 0)
        {
            return;
        }

        string parent = assetPath.Substring(0, slashIndex);
        string folderName = assetPath.Substring(slashIndex + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
