using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerDirectionalControllerInstaller
{
    private const string TargetFolderPath = "Assets/SourceFiles/Animation";
    private const string TargetControllerPath = "Assets/SourceFiles/Animation/PlayerRobotDirectional.controller";
    private const string LocomotionTreeName = "PlayerDirectionalLocomotion";
    private const string SessionKey = "PlayerDirectionalControllerInstaller.Ran";

    private static readonly string[] PlayerPrefabPaths =
    {
        "Assets/Prefabs/PlayerRobot.prefab",
        "Assets/Resources/Bootstrap/PlayerRobot.prefab"
    };

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += TryInstall;
    }

    [MenuItem("Tools/Arena/Install Player Directional Controller")]
    public static void InstallFromMenu()
    {
        InstallOrUpdate();
    }

    private static void TryInstall()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Kevin Iglesias/Human Animations"))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);

        try
        {
            InstallOrUpdate();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Directional controller install failed: {exception}");
        }
    }

    private static void InstallOrUpdate()
    {
        EnsureFolder(TargetFolderPath);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(TargetControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(TargetControllerPath);
        }

        if (controller == null)
        {
            throw new InvalidOperationException("Could not load directional controller.");
        }

        ResetController(controller);
        EnsureFloatParameter(controller, "MoveX");
        EnsureFloatParameter(controller, "MoveY");
        EnsureFloatParameter(controller, "Speed");
        EnsureFloatParameter(controller, "MotionSpeed");
        EnsureBoolParameter(controller, "Grounded");
        EnsureBoolParameter(controller, "Jump");
        EnsureBoolParameter(controller, "FreeFall");
        EnsureTriggerParameter(controller, "Roll");

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        BlendTree locomotionTree = GetOrCreateLocomotionTree(controller);
        RebuildLocomotionTree(locomotionTree);

        AnimatorState locomotionState = stateMachine.AddState("Locomotion", new Vector3(300f, 100f, 0f));
        locomotionState.motion = locomotionTree;
        locomotionState.iKOnFeet = true;

        AnimatorState jumpStartState = stateMachine.AddState("JumpStart", new Vector3(300f, 250f, 0f));
        jumpStartState.motion = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Jump01 - Begin.fbx", "HumanM@Jump01 - Begin");
        jumpStartState.iKOnFeet = true;

        AnimatorState inAirState = stateMachine.AddState("InAir", new Vector3(300f, 400f, 0f));
        inAirState.motion = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Fall01.fbx", "HumanM@Fall01");
        inAirState.iKOnFeet = true;

        AnimatorState landState = stateMachine.AddState("Land", new Vector3(500f, 250f, 0f));
        landState.motion = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Jump01 - Land.fbx", "HumanM@Jump01 - Land");
        landState.iKOnFeet = true;

        AnimatorState rollState = stateMachine.AddState("Roll", new Vector3(500f, 100f, 0f));
        rollState.motion = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/HumanM@Roll01.fbx", "HumanM@Roll01");
        rollState.iKOnFeet = true;

        stateMachine.defaultState = locomotionState;

        AnimatorStateTransition jumpTransition = locomotionState.AddTransition(jumpStartState);
        jumpTransition.hasExitTime = false;
        jumpTransition.duration = 0.08f;
        jumpTransition.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

        AnimatorStateTransition freeFallTransition = locomotionState.AddTransition(inAirState);
        freeFallTransition.hasExitTime = false;
        freeFallTransition.duration = 0.08f;
        freeFallTransition.AddCondition(AnimatorConditionMode.If, 0f, "FreeFall");

        AnimatorStateTransition jumpToAirTransition = jumpStartState.AddTransition(inAirState);
        jumpToAirTransition.hasExitTime = true;
        jumpToAirTransition.exitTime = 0.9f;
        jumpToAirTransition.duration = 0.05f;

        AnimatorStateTransition airToLandTransition = inAirState.AddTransition(landState);
        airToLandTransition.hasExitTime = false;
        airToLandTransition.duration = 0.08f;
        airToLandTransition.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");

        AnimatorStateTransition landToLocomotionTransition = landState.AddTransition(locomotionState);
        landToLocomotionTransition.hasExitTime = true;
        landToLocomotionTransition.exitTime = 0.8f;
        landToLocomotionTransition.duration = 0.08f;

        AnimatorStateTransition anyToRollTransition = stateMachine.AddAnyStateTransition(rollState);
        anyToRollTransition.hasExitTime = false;
        anyToRollTransition.duration = 0.03f;
        anyToRollTransition.AddCondition(AnimatorConditionMode.If, 0f, "Roll");

        AnimatorStateTransition rollToLocomotionTransition = rollState.AddTransition(locomotionState);
        rollToLocomotionTransition.hasExitTime = true;
        rollToLocomotionTransition.exitTime = 0.95f;
        rollToLocomotionTransition.duration = 0.05f;

        AssignControllerToPlayerPrefabs(controller);

        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(locomotionState);
        EditorUtility.SetDirty(jumpStartState);
        EditorUtility.SetDirty(inAirState);
        EditorUtility.SetDirty(landState);
        EditorUtility.SetDirty(rollState);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ResetController(AnimatorController controller)
    {
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        while (stateMachine.states.Length > 0)
        {
            stateMachine.RemoveState(stateMachine.states[0].state);
        }

        while (stateMachine.anyStateTransitions.Length > 0)
        {
            stateMachine.RemoveAnyStateTransition(stateMachine.anyStateTransitions[0]);
        }
    }

    private static BlendTree GetOrCreateLocomotionTree(AnimatorController controller)
    {
        BlendTree tree = AssetDatabase.LoadAllAssetsAtPath(TargetControllerPath)
            .OfType<BlendTree>()
            .FirstOrDefault(candidate => candidate.name == LocomotionTreeName);

        if (tree != null)
        {
            return tree;
        }

        tree = new BlendTree
        {
            name = LocomotionTreeName
        };

        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    private static void RebuildLocomotionTree(BlendTree tree)
    {
        tree.blendType = BlendTreeType.FreeformDirectional2D;
        tree.blendParameter = "MoveX";
        tree.blendParameterY = "MoveY";
        tree.useAutomaticThresholds = false;

        tree.children = Array.Empty<ChildMotion>();

        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx", "HumanM@Idle01"), Vector2.zero);

        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx", "HumanM@Walk01_Forward"), new Vector2(0f, 1f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Backward.fbx", "HumanM@Walk01_Backward"), new Vector2(0f, -1f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_Left.fbx", "HumanM@StrafeWalk01_Left"), new Vector2(-1f, 0f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_Right.fbx", "HumanM@StrafeWalk01_Right"), new Vector2(1f, 0f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_ForwardLeft.fbx", "HumanM@StrafeWalk01_ForwardLeft"), new Vector2(-1f, 1f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_ForwardRight.fbx", "HumanM@StrafeWalk01_ForwardRight"), new Vector2(1f, 1f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_BackwardLeft.fbx", "HumanM@StrafeWalk01_BackwardLeft"), new Vector2(-1f, -1f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_BackwardRight.fbx", "HumanM@StrafeWalk01_BackwardRight"), new Vector2(1f, -1f));

        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx", "HumanM@Run01_Forward"), new Vector2(0f, 2f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Backward.fbx", "HumanM@Run01_Backward"), new Vector2(0f, -2f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_Left.fbx", "HumanM@StrafeRun01_Left"), new Vector2(-2f, 0f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_Right.fbx", "HumanM@StrafeRun01_Right"), new Vector2(2f, 0f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_ForwardLeft.fbx", "HumanM@StrafeRun01_ForwardLeft"), new Vector2(-2f, 2f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_ForwardRight.fbx", "HumanM@StrafeRun01_ForwardRight"), new Vector2(2f, 2f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_BackwardLeft.fbx", "HumanM@StrafeRun01_BackwardLeft"), new Vector2(-2f, -2f));
        tree.AddChild(LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_BackwardRight.fbx", "HumanM@StrafeRun01_BackwardRight"), new Vector2(2f, -2f));

        EditorUtility.SetDirty(tree);
    }

    private static Motion LoadClip(string assetPath, string clipName)
    {
        Motion motion = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Motion>()
            .FirstOrDefault(candidate => candidate.name == clipName);

        if (motion == null)
        {
            throw new InvalidOperationException($"Could not find clip '{clipName}' at '{assetPath}'.");
        }

        return motion;
    }

    private static void AssignControllerToPlayerPrefabs(RuntimeAnimatorController controller)
    {
        foreach (string prefabPath in PlayerPrefabPaths)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                continue;
            }

            try
            {
                Animator animator = prefabRoot.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    continue;
                }

                animator.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static void EnsureFloatParameter(AnimatorController controller, string parameterName)
    {
        if (controller.parameters.Any(parameter => parameter.name == parameterName))
        {
            return;
        }

        controller.AddParameter(parameterName, AnimatorControllerParameterType.Float);
    }

    private static void EnsureBoolParameter(AnimatorController controller, string parameterName)
    {
        if (controller.parameters.Any(parameter => parameter.name == parameterName))
        {
            return;
        }

        controller.AddParameter(parameterName, AnimatorControllerParameterType.Bool);
    }

    private static void EnsureTriggerParameter(AnimatorController controller, string parameterName)
    {
        if (controller.parameters.Any(parameter => parameter.name == parameterName))
        {
            return;
        }

        controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        int separatorIndex = folderPath.LastIndexOf('/');
        string parentFolder = folderPath.Substring(0, separatorIndex);
        string folderName = folderPath.Substring(separatorIndex + 1);
        EnsureFolder(parentFolder);
        AssetDatabase.CreateFolder(parentFolder, folderName);
    }
}
