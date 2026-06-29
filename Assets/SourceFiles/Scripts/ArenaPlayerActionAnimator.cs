using StarterAssets;
using UnityEngine;

[DefaultExecutionOrder(60)]
[RequireComponent(typeof(ThirdPersonController))]
public class ArenaPlayerActionAnimator : MonoBehaviour
{
    private ThirdPersonController controller;
    private ArenaThrowClipPlayer throwClipPlayer;
    private Transform actorRoot;
    private Transform hips;
    private Transform spine;
    private Transform chest;
    private Transform head;
    private Transform leftShoulder;
    private Transform leftUpperArm;
    private Transform leftLowerArm;
    private Transform rightShoulder;
    private Transform rightUpperArm;
    private Transform rightLowerArm;
    private Transform leftUpperLeg;
    private Transform leftLowerLeg;
    private Transform rightUpperLeg;
    private Transform rightLowerLeg;

    private void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
        throwClipPlayer = GetComponent<ArenaThrowClipPlayer>();
        actorRoot = transform;
        BindBones();
    }

    private void LateUpdate()
    {
        if (controller == null)
        {
            return;
        }

        if (throwClipPlayer != null && throwClipPlayer.IsThrowing)
        {
            return;
        }

        if (controller.IsRolling)
        {
            ApplyRollPose(controller.RollPoseWeight);
            return;
        }

        if (controller.IsBlocking)
        {
            ApplyBlockPose(1f);
            return;
        }

        if (controller.DoubleJumpPoseWeight > 0f)
        {
            ApplyDoubleJumpPose(controller.DoubleJumpPoseWeight);
        }
    }

    private void BindBones()
    {
        hips = FindBone("Hips", "Pelvis");
        spine = FindBone("Spine", "Body", "Core");
        chest = FindBone("Chest", "UpperChest", "Torso");
        head = FindBone("Head");
        leftShoulder = FindBone("Left_Shoulder", "LeftShoulder");
        leftUpperArm = FindBone("Left_UpperArm", "LeftArm");
        leftLowerArm = FindBone("Left_LowerArm", "LeftForeArm", "LeftForearm");
        rightShoulder = FindBone("Right_Shoulder", "RightShoulder");
        rightUpperArm = FindBone("Right_UpperArm", "RightArm");
        rightLowerArm = FindBone("Right_LowerArm", "RightForeArm", "RightForearm");
        leftUpperLeg = FindBone("LeftLeg", "Left_UpperLeg", "LeftUpperLeg");
        leftLowerLeg = FindBone("LeftKnee", "Left_LowerLeg", "LeftLowerLeg");
        rightUpperLeg = FindBone("RightLeg", "Right_UpperLeg", "RightUpperLeg");
        rightLowerLeg = FindBone("RightKnee", "Right_LowerLeg", "RightLowerLeg");
    }

    private void ApplyBlockPose(float weight)
    {
        ApplyOffset(chest, new Vector3(-6f, 0f, 0f), weight);
        ApplyOffset(head, new Vector3(4f, 0f, 0f), weight);
        ApplyOffset(leftShoulder, new Vector3(-18f, 22f, 28f), weight);
        ApplyOffset(leftUpperArm, new Vector3(-42f, 36f, 58f), weight);
        ApplyOffset(leftLowerArm, new Vector3(-56f, 8f, 18f), weight);
        ApplyOffset(rightShoulder, new Vector3(-18f, -22f, -28f), weight);
        ApplyOffset(rightUpperArm, new Vector3(-42f, -36f, -58f), weight);
        ApplyOffset(rightLowerArm, new Vector3(-56f, -8f, -18f), weight);
    }

    private void ApplyRollPose(float weight)
    {
        ApplyOffset(hips, new Vector3(20f, 0f, 0f), weight);
        ApplyOffset(spine, new Vector3(-34f, 0f, 0f), weight);
        ApplyOffset(chest, new Vector3(-28f, 0f, 0f), weight);
        ApplyOffset(head, new Vector3(-22f, 0f, 0f), weight);
        ApplyOffset(leftShoulder, new Vector3(-44f, 30f, 34f), weight);
        ApplyOffset(leftUpperArm, new Vector3(-68f, 42f, 60f), weight);
        ApplyOffset(leftLowerArm, new Vector3(-66f, 16f, 20f), weight);
        ApplyOffset(rightShoulder, new Vector3(-44f, -30f, -34f), weight);
        ApplyOffset(rightUpperArm, new Vector3(-68f, -42f, -60f), weight);
        ApplyOffset(rightLowerArm, new Vector3(-66f, -16f, -20f), weight);
        ApplyOffset(leftUpperLeg, new Vector3(54f, 0f, 0f), weight);
        ApplyOffset(leftLowerLeg, new Vector3(-82f, 0f, 0f), weight);
        ApplyOffset(rightUpperLeg, new Vector3(54f, 0f, 0f), weight);
        ApplyOffset(rightLowerLeg, new Vector3(-82f, 0f, 0f), weight);
    }

    private void ApplyDoubleJumpPose(float weight)
    {
        ApplyOffset(chest, new Vector3(10f, 0f, 0f), weight);
        ApplyOffset(leftShoulder, new Vector3(-10f, 26f, 38f), weight);
        ApplyOffset(leftUpperArm, new Vector3(-22f, 42f, 70f), weight);
        ApplyOffset(rightShoulder, new Vector3(-10f, -26f, -38f), weight);
        ApplyOffset(rightUpperArm, new Vector3(-22f, -42f, -70f), weight);
        ApplyOffset(leftUpperLeg, new Vector3(18f, 0f, 0f), weight);
        ApplyOffset(rightUpperLeg, new Vector3(18f, 0f, 0f), weight);
    }

    private void ApplyOffset(Transform bone, Vector3 eulerOffset, float weight)
    {
        if (bone == null || weight <= 0f)
        {
            return;
        }

        Quaternion offset = Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Euler(eulerOffset), weight);
        bone.localRotation = bone.localRotation * offset;
    }

    private Transform FindBone(params string[] names)
    {
        foreach (string name in names)
        {
            Transform bone = MiniGameManager.FindChildRecursive(actorRoot, name);
            if (bone != null)
            {
                return bone;
            }
        }

        return null;
    }
}
