#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class ArenaSceneRuntimeSetupInstaller
{
    private const string PlayFieldRootName = "PlayField";
    private const string ArenaRootName = "Arena";
    private const string HudPrefabPath = "Assets/SourceFiles/Resources/UI/ArenaHudCanvas.prefab";
    private const string MenuPrefabPath = "Assets/SourceFiles/Resources/UI/CameraSensitivityMenuCanvas.prefab";
    private const string LooseBallPrefabPath = "Assets/SourceFiles/Resources/Arena/ArenaBallRuntime.prefab";
    private const string ProjectileBallPrefabPath = "Assets/SourceFiles/Resources/Arena/ArenaBallProjectile.prefab";
    [MenuItem("Tools/Arena/Install Scene Runtime Setup")]
    public static void Install()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        bool changed = false;
        changed |= EnsureSceneComponent<MiniGameManager>("MiniGameManager");
        changed |= EnsureSceneComponent<CameraSensitivityMenu>("CameraSensitivityMenu");
        changed |= EnsureEventSystem();
        changed |= EnsurePrefabInstance<ArenaHudView>(HudPrefabPath, "ArenaHudCanvas");
        changed |= EnsurePrefabInstance<CameraSensitivityMenuView>(MenuPrefabPath, "CameraSensitivityMenuCanvas", deactivateAfterCreate: true);
        changed |= EnsurePrefabInstance<ArenaBallPickup>(LooseBallPrefabPath, "ArenaBallPickup", deactivateAfterCreate: true);
        changed |= EnsurePrefabInstance<ArenaProjectile>(ProjectileBallPrefabPath, "ArenaBallProjectile", deactivateAfterCreate: true);
        changed |= EnsurePlayFieldColliderSetup();
        changed |= NormalizeArenaBallVisuals();
        changed |= AssignSceneReferences();

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Arena scene runtime setup installed in the active scene.");
        }
        else
        {
            Debug.Log("Arena scene runtime setup was already present in the active scene.");
        }
    }

    private static bool EnsureSceneComponent<T>(string objectName) where T : Component
    {
        if (Object.FindAnyObjectByType<T>(FindObjectsInactive.Include) != null)
        {
            return false;
        }

        GameObject root = new GameObject(objectName);
        root.AddComponent<T>();
        Undo.RegisterCreatedObjectUndo(root, $"Create {objectName}");
        return true;
    }

    private static bool EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        bool changed = false;

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
            changed = true;
        }

        BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
        foreach (BaseInputModule module in modules)
        {
            if (module is InputSystemUIInputModule)
            {
                continue;
            }

            Undo.DestroyObjectImmediate(module);
            changed = true;
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            changed = true;
        }

        return changed;
    }

    private static bool EnsurePrefabInstance<T>(string prefabPath, string objectName, bool deactivateAfterCreate = false) where T : Component
    {
        if (Object.FindAnyObjectByType<T>(FindObjectsInactive.Include) != null)
        {
            return false;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new System.InvalidOperationException($"Could not load prefab at '{prefabPath}'.");
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            throw new System.InvalidOperationException($"Could not instantiate prefab at '{prefabPath}'.");
        }

        instance.name = objectName;
        if (deactivateAfterCreate)
        {
            instance.SetActive(false);
        }

        Undo.RegisterCreatedObjectUndo(instance, $"Create {objectName}");
        return true;
    }

    private static bool EnsurePlayFieldColliderSetup()
    {
        GameObject playFieldObject = GameObject.Find(PlayFieldRootName);
        if (playFieldObject == null)
        {
            throw new System.InvalidOperationException($"Could not find '{PlayFieldRootName}' in the active scene.");
        }

        bool changed = false;
        Transform playField = playFieldObject.transform;

        Collider playFieldCollider = playFieldObject.GetComponent<Collider>();
        if (playFieldCollider == null)
        {
            if (!TryCalculateBounds(playField, out Bounds worldBounds))
            {
                throw new System.InvalidOperationException("Could not calculate PlayField bounds to create its scene collider.");
            }

            BoxCollider boxCollider = Undo.AddComponent<BoxCollider>(playFieldObject);
            Vector3 localCenter = playField.InverseTransformPoint(worldBounds.center);
            Vector3 lossyScale = playField.lossyScale;
            float scaleX = Mathf.Approximately(lossyScale.x, 0f) ? 1f : Mathf.Abs(lossyScale.x);
            float scaleY = Mathf.Approximately(lossyScale.y, 0f) ? 1f : Mathf.Abs(lossyScale.y);
            float scaleZ = Mathf.Approximately(lossyScale.z, 0f) ? 1f : Mathf.Abs(lossyScale.z);
            boxCollider.center = localCenter;
            boxCollider.size = new Vector3(
                worldBounds.size.x / scaleX,
                worldBounds.size.y / scaleY,
                worldBounds.size.z / scaleZ);
            boxCollider.isTrigger = false;
            changed = true;
            playFieldCollider = boxCollider;
        }

        if (playFieldCollider.isTrigger)
        {
            playFieldCollider.isTrigger = false;
            changed = true;
        }

        if (!TryCalculateBounds(playField, out Bounds bounds))
        {
            bounds = playFieldCollider.bounds;
        }

        return changed;
    }

    private static bool AssignSceneReferences()
    {
        bool changed = false;

        MiniGameManager miniGameManager = Object.FindAnyObjectByType<MiniGameManager>(FindObjectsInactive.Include);
        CameraSensitivityMenu cameraMenu = Object.FindAnyObjectByType<CameraSensitivityMenu>(FindObjectsInactive.Include);
        ArenaBallService ballService = miniGameManager != null ? miniGameManager.GetComponent<ArenaBallService>() : null;
        ArenaHudView hudView = Object.FindAnyObjectByType<ArenaHudView>(FindObjectsInactive.Include);
        CameraSensitivityMenuView menuView = Object.FindAnyObjectByType<CameraSensitivityMenuView>(FindObjectsInactive.Include);
        ArenaBallPickup looseBall = Object.FindAnyObjectByType<ArenaBallPickup>(FindObjectsInactive.Include);
        ArenaProjectile projectileBall = Object.FindAnyObjectByType<ArenaProjectile>(FindObjectsInactive.Include);

        Transform arenaRoot = FindSceneTransform(ArenaRootName);
        Transform playFieldRoot = FindSceneTransform(PlayFieldRootName);
        Light directionalLight = FindDirectionalLight();
        Collider playFieldCollider = playFieldRoot != null ? playFieldRoot.GetComponent<Collider>() : null;
        Collider[] invisibleWalls = FindInvisibleWalls();

        if (miniGameManager != null)
        {
            changed |= SetObjectReference(miniGameManager, "arenaRoot", arenaRoot);
            changed |= SetObjectReference(miniGameManager, "playFieldRoot", playFieldRoot);
            changed |= SetObjectReference(miniGameManager, "playFieldSurfaceCollider", playFieldCollider);
            changed |= SetObjectReference(miniGameManager, "arenaDirectionalLight", directionalLight);
            changed |= SetObjectReference(miniGameManager, "hudView", hudView);
            changed |= SetObjectReferenceArray(miniGameManager, "invisibleWalls", invisibleWalls);
        }

        if (cameraMenu != null)
        {
            changed |= SetObjectReference(cameraMenu, "menuView", menuView);
        }

        if (ballService != null)
        {
            changed |= SetObjectReference(ballService, "sceneLooseBall", looseBall);
            changed |= SetObjectReference(ballService, "sceneProjectileBall", projectileBall);
        }

        return changed;
    }

    private static bool NormalizeArenaBallVisuals()
    {
        bool changed = false;

        ArenaBallPickup looseBall = Object.FindAnyObjectByType<ArenaBallPickup>(FindObjectsInactive.Include);
        if (looseBall != null)
        {
            changed |= ArenaBallAssetInstaller.AlignVisualToRoot(looseBall.gameObject);
        }

        ArenaProjectile projectileBall = Object.FindAnyObjectByType<ArenaProjectile>(FindObjectsInactive.Include);
        if (projectileBall != null)
        {
            changed |= ArenaBallAssetInstaller.AlignVisualToRoot(projectileBall.gameObject);
        }

        return changed;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null ? target.transform : null;
    }

    private static Light FindDirectionalLight()
    {
        foreach (Light lightComponent in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            if (lightComponent != null && lightComponent.type == LightType.Directional)
            {
                return lightComponent;
            }
        }

        return null;
    }

    private static Collider[] FindInvisibleWalls()
    {
        System.Collections.Generic.List<Collider> walls = new System.Collections.Generic.List<Collider>();
        foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))
        {
            if (collider != null && collider.gameObject.name.StartsWith("InvisibleWall"))
            {
                walls.Add(collider);
            }
        }

        return walls.ToArray();
    }

    private static bool SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == value)
        {
            return false;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        return true;
    }

    private static bool SetObjectReferenceArray(Object target, string propertyName, Object[] values)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return false;
        }

        bool changed = property.arraySize != values.Length;
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue != values[i])
            {
                element.objectReferenceValue = values[i];
                changed = true;
            }
        }

        if (!changed)
        {
            return false;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        return true;
    }

    private static bool TryCalculateBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        if (root == null)
        {
            return false;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }
}
#endif
