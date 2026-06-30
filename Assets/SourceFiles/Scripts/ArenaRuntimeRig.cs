using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class ArenaRuntimeRig : MonoBehaviour
{
    private const int ActivePlayerCameraPriority = 100;
    private const int InactiveCameraPriority = 0;

    private Transform sceneRoot;
    private CinemachineCamera followCamera;
    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInputs;
    private PlayerInput playerInput;

    public void Initialize(Transform runtimeSceneRoot, bool isPlayerControlled)
    {
        sceneRoot = runtimeSceneRoot != null ? runtimeSceneRoot : transform.root;
        CacheReferences();
        ConfigurePlayerControl(isPlayerControlled);
        ConfigureCameraRig(isPlayerControlled);
    }

    private void CacheReferences()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        playerInput = GetComponent<PlayerInput>();
        followCamera = ResolveFollowCamera();

        if (thirdPersonController != null && thirdPersonController.CinemachineCameraTarget == null)
        {
            Transform cameraTarget = FindCameraTarget();
            if (cameraTarget != null)
            {
                thirdPersonController.CinemachineCameraTarget = cameraTarget.gameObject;
            }
        }

        if (playerInput != null)
        {
            playerInput.camera = Camera.main;
        }
    }

    private void ConfigurePlayerControl(bool isPlayerControlled)
    {
        SetBehaviourEnabled(thirdPersonController, isPlayerControlled);
        SetBehaviourEnabled(starterAssetsInputs, isPlayerControlled);
        SetBehaviourEnabled(playerInput, isPlayerControlled);
    }

    private void ConfigureCameraRig(bool isPlayerControlled)
    {
        if (followCamera != null)
        {
            followCamera.enabled = isPlayerControlled;
            followCamera.Priority = isPlayerControlled ? ActivePlayerCameraPriority : InactiveCameraPriority;
        }
    }

    private CinemachineCamera ResolveFollowCamera()
    {
        if (sceneRoot == null)
        {
            return null;
        }

        Transform followCameraTransform = MiniGameManager.FindChildRecursive(sceneRoot, "PlayerFollowCamera");
        return followCameraTransform != null ? followCameraTransform.GetComponent<CinemachineCamera>() : null;
    }

    private Transform FindCameraTarget()
    {
        if (sceneRoot == null)
        {
            return null;
        }

        foreach (Transform child in sceneRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("CinemachineTarget") || child.name == "PlayerCameraRoot")
            {
                return child;
            }
        }

        return null;
    }

    private static void SetBehaviourEnabled(Behaviour behaviour, bool isEnabled)
    {
        if (behaviour != null)
        {
            behaviour.enabled = isEnabled;
        }
    }
}
