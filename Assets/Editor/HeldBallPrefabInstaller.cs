#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class HeldBallPrefabInstaller
{
    private const string BallVisualPrefabPath = "Assets/MarpaStudio/Built-In/Prefabs/BasketBall.prefab";
    private const float HeldBallScale = 0.72f;
    private static readonly Vector3 HeldBallAnchorLocalPosition = new Vector3(0.02f, 0.01f, 0.16f);
    private static readonly Vector3 ThrowOriginLocalPosition = new Vector3(0.02f, 0.01f, 0.18f);

    [MenuItem("Tools/Arena/Rebuild Held Ball Prefabs")]
    public static void Rebuild()
    {
        UpdateHeldBallPrefab("Assets/Prefabs/PlayerRobot.prefab");
        UpdateHeldBallPrefab("Assets/Resources/Bootstrap/PlayerRobot.prefab");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void UpdateHeldBallPrefab(string prefabPath)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform hand = FindPreferredRightHand(prefabRoot.transform);
            if (hand == null)
            {
                throw new System.InvalidOperationException($"Could not find right hand in prefab '{prefabPath}'.");
            }

            Transform heldBallAnchor = EnsureChild(hand, "HeldBallAnchor");
            heldBallAnchor.localPosition = HeldBallAnchorLocalPosition;
            heldBallAnchor.localRotation = Quaternion.identity;
            heldBallAnchor.localScale = Vector3.one;

            Transform throwOrigin = EnsureChild(hand, "ThrowOrigin");
            throwOrigin.localPosition = ThrowOriginLocalPosition;
            throwOrigin.localRotation = Quaternion.identity;
            throwOrigin.localScale = Vector3.one;

            GameObject existingHeldBall = FindChildRecursive(heldBallAnchor, "HeldArenaBall")?.gameObject;
            if (existingHeldBall != null)
            {
                Object.DestroyImmediate(existingHeldBall);
            }

            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BallVisualPrefabPath);
            if (visualPrefab == null)
            {
                throw new System.InvalidOperationException($"Could not load ball visual prefab at '{BallVisualPrefabPath}'.");
            }

            GameObject heldBall = PrefabUtility.InstantiatePrefab(visualPrefab) as GameObject;
            if (heldBall == null)
            {
                throw new System.InvalidOperationException($"Could not instantiate held ball for '{prefabPath}'.");
            }

            heldBall.name = "HeldArenaBall";
            heldBall.transform.SetParent(heldBallAnchor, false);
            heldBall.transform.localPosition = Vector3.zero;
            heldBall.transform.localRotation = Quaternion.identity;
            heldBall.transform.localScale = Vector3.one * HeldBallScale;
            heldBall.SetActive(false);

            foreach (Collider collider in heldBall.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }

            ArenaBallAttachmentPoints attachmentPoints = prefabRoot.GetComponent<ArenaBallAttachmentPoints>();
            if (attachmentPoints == null)
            {
                attachmentPoints = prefabRoot.AddComponent<ArenaBallAttachmentPoints>();
            }

            SerializedObject serializedObject = new SerializedObject(attachmentPoints);
            serializedObject.FindProperty("heldBallAnchor").objectReferenceValue = heldBallAnchor;
            serializedObject.FindProperty("throwOrigin").objectReferenceValue = throwOrigin;
            serializedObject.FindProperty("heldBallVisual").objectReferenceValue = heldBall;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static Transform FindPreferredRightHand(Transform root)
    {
        Transform handProp = FindChildRecursive(root, "B-handProp.R");
        if (handProp != null)
        {
            return handProp;
        }

        Transform hand = FindChildRecursive(root, "Right_Hand");
        if (hand != null)
        {
            return hand;
        }

        hand = FindChildRecursive(root, "RightHand");
        if (hand != null)
        {
            return hand;
        }

        return FindChildRecursive(root, "B-hand.R");
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
#endif
