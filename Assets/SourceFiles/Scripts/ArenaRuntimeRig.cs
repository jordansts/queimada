using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArenaRuntimeRig : MonoBehaviour
{
    private Transform sceneRoot;

    public void Initialize(Transform runtimeSceneRoot, bool isPlayerControlled)
    {
        sceneRoot = runtimeSceneRoot != null ? runtimeSceneRoot : transform.root;
        SetComponentEnabled<ThirdPersonController>(isPlayerControlled);
        SetComponentEnabled<StarterAssetsInputs>(isPlayerControlled);
        SetComponentEnabled<PlayerInput>(isPlayerControlled);
        SetCameraRigActive(isPlayerControlled);
    }

    private void SetCameraRigActive(bool isActive)
    {
        if (sceneRoot == null)
        {
            return;
        }

        Camera primaryCamera = null;
        foreach (Camera cameraComponent in sceneRoot.GetComponentsInChildren<Camera>(true))
        {
            if (cameraComponent != null && cameraComponent.name == "RobotCamera")
            {
                primaryCamera = cameraComponent;
                break;
            }
        }

        if (primaryCamera == null)
        {
            primaryCamera = sceneRoot.GetComponentInChildren<Camera>(true);
        }

        foreach (Camera cameraComponent in sceneRoot.GetComponentsInChildren<Camera>(true))
        {
            if (cameraComponent == null)
            {
                continue;
            }

            cameraComponent.enabled = isActive;
            cameraComponent.gameObject.SetActive(isActive);
            cameraComponent.gameObject.tag = isActive && cameraComponent == primaryCamera
                ? "MainCamera"
                : "Untagged";
        }

        foreach (AudioListener listener in sceneRoot.GetComponentsInChildren<AudioListener>(true))
        {
            if (listener != null)
            {
                listener.enabled = isActive;
            }
        }

        Transform followCamera = MiniGameManager.FindChildRecursive(sceneRoot, "PlayerFollowCamera");
        if (followCamera != null)
        {
            followCamera.gameObject.SetActive(isActive);
        }

        if (!isActive)
        {
            return;
        }

        foreach (Camera cameraComponent in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cameraComponent == null || cameraComponent.transform.IsChildOf(sceneRoot))
            {
                continue;
            }

            if (cameraComponent.CompareTag("MainCamera"))
            {
                cameraComponent.gameObject.tag = "Untagged";
            }

            if (cameraComponent.name == "Main Camera" || cameraComponent.name == "FallbackMainCamera")
            {
                cameraComponent.enabled = false;
            }
        }
    }

    private void SetComponentEnabled<T>(bool isEnabled) where T : Behaviour
    {
        T component = GetComponent<T>();
        if (component != null)
        {
            component.enabled = isEnabled;
        }
    }
}
