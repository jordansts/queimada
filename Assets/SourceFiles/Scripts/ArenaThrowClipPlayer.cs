using UnityEngine;

[DefaultExecutionOrder(50)]
public class ArenaThrowClipPlayer : MonoBehaviour
{
    [SerializeField] private float throwDuration = 0.42f;
    [SerializeField] private float recoveryDuration = 0.22f;

    public float ReleaseDelay => throwDuration * 0.78f;
    public bool IsThrowing => isThrowing;

    private Transform actorRoot;
    private Transform hips;
    private Transform spine;
    private Transform chest;
    private Transform head;
    private Transform rightShoulder;
    private Transform rightUpperArm;
    private Transform rightLowerArm;
    private Transform rightHand;
    private Transform leftShoulder;
    private Transform leftUpperArm;
    private Transform leftLowerArm;

    private Quaternion hipsBaseRotation;
    private Quaternion spineBaseRotation;
    private Quaternion chestBaseRotation;
    private Quaternion headBaseRotation;
    private Quaternion rightShoulderBaseRotation;
    private Quaternion rightUpperArmBaseRotation;
    private Quaternion rightLowerArmBaseRotation;
    private Quaternion rightHandBaseRotation;
    private Quaternion leftShoulderBaseRotation;
    private Quaternion leftUpperArmBaseRotation;
    private Quaternion leftLowerArmBaseRotation;

    private float throwTimer = -1f;
    private bool isThrowing;

    public void Initialize(Transform actorRoot)
    {
        this.actorRoot = actorRoot != null ? actorRoot : transform;
        BindBones();
    }

    public void PlayThrow()
    {
        if (actorRoot == null)
        {
            Initialize(transform);
        }

        if (rightShoulder == null || rightUpperArm == null || rightLowerArm == null || rightHand == null)
        {
            return;
        }

        CacheBasePose();
        throwTimer = 0f;
        isThrowing = true;
    }

    private void LateUpdate()
    {
        if (!isThrowing)
        {
            return;
        }

        throwTimer += Time.deltaTime;
        float totalDuration = throwDuration + recoveryDuration;
        float normalizedTime = Mathf.Clamp01(throwTimer / totalDuration);

        if (throwTimer >= totalDuration)
        {
            isThrowing = false;
            return;
        }

        ApplyThrowPose(normalizedTime);
    }

    private void BindBones()
    {
        hips = FindFirstExistingChild("Hips", "Pelvis", "B-hips");
        spine = FindFirstExistingChild("Spine", "Body", "Core", "B-spine");
        chest = FindFirstExistingChild("Chest", "UpperChest", "Torso", "B-chest");
        head = FindFirstExistingChild("Head", "B-head");
        rightShoulder = FindFirstExistingChild("Right_Shoulder", "RightShoulder", "B-shoulder.R");
        rightUpperArm = FindFirstExistingChild("Right_UpperArm", "RightArm", "B-upperArm.R");
        rightLowerArm = FindFirstExistingChild("Right_LowerArm", "RightForeArm", "RightForearm", "B-forearm.R");
        rightHand = FindFirstExistingChild("Right_Hand", "RightHand", "B-hand.R");
        leftShoulder = FindFirstExistingChild("Left_Shoulder", "LeftShoulder", "B-shoulder.L");
        leftUpperArm = FindFirstExistingChild("Left_UpperArm", "LeftArm", "B-upperArm.L");
        leftLowerArm = FindFirstExistingChild("Left_LowerArm", "LeftForeArm", "LeftForearm", "B-forearm.L");
    }

    private Transform FindFirstExistingChild(params string[] names)
    {
        if (actorRoot == null)
        {
            return null;
        }

        foreach (string boneName in names)
        {
            Transform bone = MiniGameManager.FindChildRecursive(actorRoot, boneName);
            if (bone != null)
            {
                return bone;
            }
        }

        return null;
    }

    private void CacheBasePose()
    {
        hipsBaseRotation = hips != null ? hips.localRotation : Quaternion.identity;
        spineBaseRotation = spine != null ? spine.localRotation : Quaternion.identity;
        chestBaseRotation = chest != null ? chest.localRotation : Quaternion.identity;
        headBaseRotation = head != null ? head.localRotation : Quaternion.identity;
        rightShoulderBaseRotation = rightShoulder.localRotation;
        rightUpperArmBaseRotation = rightUpperArm.localRotation;
        rightLowerArmBaseRotation = rightLowerArm.localRotation;
        rightHandBaseRotation = rightHand.localRotation;
        leftShoulderBaseRotation = leftShoulder != null ? leftShoulder.localRotation : Quaternion.identity;
        leftUpperArmBaseRotation = leftUpperArm != null ? leftUpperArm.localRotation : Quaternion.identity;
        leftLowerArmBaseRotation = leftLowerArm != null ? leftLowerArm.localRotation : Quaternion.identity;
    }

    private void ApplyThrowPose(float normalizedTime)
    {
        Vector3 hipsOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(-6f, -22f, -4f),
            new Vector3(8f, 30f, 6f),
            Vector3.zero);

        Vector3 spineOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(-14f, -46f, -10f),
            new Vector3(16f, 56f, 14f),
            Vector3.zero);

        Vector3 chestOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(-22f, -68f, -14f),
            new Vector3(28f, 82f, 16f),
            Vector3.zero);

        Vector3 headOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(6f, -16f, 0f),
            new Vector3(-8f, 18f, 0f),
            Vector3.zero);

        Vector3 shoulderOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(-42f, -74f, -62f),
            new Vector3(104f, 58f, 28f),
            Vector3.zero);

        Vector3 upperArmOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(-96f, -120f, -82f),
            new Vector3(148f, 18f, 70f),
            Vector3.zero);

        Vector3 lowerArmOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(-54f, -34f, -24f),
            new Vector3(132f, 12f, 34f),
            Vector3.zero);

        Vector3 handOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(-34f, -14f, -30f),
            new Vector3(68f, 8f, 42f),
            Vector3.zero);

        Vector3 leftShoulderOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(26f, 36f, 22f),
            new Vector3(-40f, -20f, -20f),
            Vector3.zero);

        Vector3 leftUpperArmOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(34f, 54f, 30f),
            new Vector3(-58f, -28f, -20f),
            Vector3.zero);

        Vector3 leftLowerArmOffset = SamplePose(
            normalizedTime,
            new Vector3(0f, 0f, 0f),
            new Vector3(18f, 22f, 10f),
            new Vector3(-28f, -10f, -8f),
            Vector3.zero);

        ApplyLocalRotation(hips, hipsBaseRotation, hipsOffset);
        ApplyLocalRotation(spine, spineBaseRotation, spineOffset);
        ApplyLocalRotation(chest, chestBaseRotation, chestOffset);
        ApplyLocalRotation(head, headBaseRotation, headOffset);
        ApplyLocalRotation(rightShoulder, rightShoulderBaseRotation, shoulderOffset);
        ApplyLocalRotation(rightUpperArm, rightUpperArmBaseRotation, upperArmOffset);
        ApplyLocalRotation(rightLowerArm, rightLowerArmBaseRotation, lowerArmOffset);
        ApplyLocalRotation(rightHand, rightHandBaseRotation, handOffset);
        ApplyLocalRotation(leftShoulder, leftShoulderBaseRotation, leftShoulderOffset);
        ApplyLocalRotation(leftUpperArm, leftUpperArmBaseRotation, leftUpperArmOffset);
        ApplyLocalRotation(leftLowerArm, leftLowerArmBaseRotation, leftLowerArmOffset);
    }

    private static Vector3 SamplePose(float normalizedTime, Vector3 start, Vector3 windup, Vector3 release, Vector3 end)
    {
        const float windupEnd = 0.44f;
        const float releaseEnd = 0.78f;

        if (normalizedTime <= windupEnd)
        {
            float t = EaseInOut(normalizedTime / windupEnd);
            return Vector3.LerpUnclamped(start, windup, t);
        }

        if (normalizedTime <= releaseEnd)
        {
            float t = EaseOutCubic((normalizedTime - windupEnd) / (releaseEnd - windupEnd));
            return Vector3.LerpUnclamped(windup, release, t);
        }

        float returnT = EaseInOut((normalizedTime - releaseEnd) / (1f - releaseEnd));
        return Vector3.LerpUnclamped(release, end, returnT);
    }

    private static void ApplyLocalRotation(Transform bone, Quaternion baseRotation, Vector3 eulerOffset)
    {
        if (bone == null)
        {
            return;
        }

        bone.localRotation = baseRotation * Quaternion.Euler(eulerOffset);
    }

    private static float EaseInOut(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }
}
