using StarterAssets;
using UnityEngine;

[DefaultExecutionOrder(60)]
[RequireComponent(typeof(ThirdPersonController))]
public class ArenaPlayerActionAnimator : MonoBehaviour
{
    private struct BoneBinding
    {
        public Transform Transform;
        public Quaternion BaseLocalRotation;

        public BoneBinding(Transform transform)
        {
            Transform = transform;
            BaseLocalRotation = transform != null ? transform.localRotation : Quaternion.identity;
        }
    }

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
    private BoneBinding[] boneBindings;

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

        RestoreBasePose();

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
        hips = FindBone("Hips", "Pelvis", "B-hips");
        spine = FindBone("Spine", "Body", "Core", "B-spine");
        chest = FindBone("Chest", "UpperChest", "Torso", "B-chest");
        head = FindBone("Head", "B-head");
        leftShoulder = FindBone("Left_Shoulder", "LeftShoulder", "B-shoulder.L");
        leftUpperArm = FindBone("Left_UpperArm", "LeftArm", "B-upperArm.L");
        leftLowerArm = FindBone("Left_LowerArm", "LeftForeArm", "LeftForearm", "B-forearm.L");
        rightShoulder = FindBone("Right_Shoulder", "RightShoulder", "B-shoulder.R");
        rightUpperArm = FindBone("Right_UpperArm", "RightArm", "B-upperArm.R");
        rightLowerArm = FindBone("Right_LowerArm", "RightForeArm", "RightForearm", "B-forearm.R");
        leftUpperLeg = FindBone("LeftLeg", "Left_UpperLeg", "LeftUpperLeg", "B-thigh.L");
        leftLowerLeg = FindBone("LeftKnee", "Left_LowerLeg", "LeftLowerLeg", "B-shin.L");
        rightUpperLeg = FindBone("RightLeg", "Right_UpperLeg", "RightUpperLeg", "B-thigh.R");
        rightLowerLeg = FindBone("RightKnee", "Right_LowerLeg", "RightLowerLeg", "B-shin.R");
        boneBindings = new[]
        {
            new BoneBinding(hips),
            new BoneBinding(spine),
            new BoneBinding(chest),
            new BoneBinding(head),
            new BoneBinding(leftShoulder),
            new BoneBinding(leftUpperArm),
            new BoneBinding(leftLowerArm),
            new BoneBinding(rightShoulder),
            new BoneBinding(rightUpperArm),
            new BoneBinding(rightLowerArm),
            new BoneBinding(leftUpperLeg),
            new BoneBinding(leftLowerLeg),
            new BoneBinding(rightUpperLeg),
            new BoneBinding(rightLowerLeg)
        };
    }

    private void RestoreBasePose()
    {
        if (boneBindings == null)
        {
            return;
        }

        for (int i = 0; i < boneBindings.Length; i++)
        {
            if (boneBindings[i].Transform != null)
            {
                boneBindings[i].Transform.localRotation = boneBindings[i].BaseLocalRotation;
            }
        }
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
