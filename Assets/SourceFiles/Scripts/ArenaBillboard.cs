using UnityEngine;

public class ArenaBillboard : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;

    public void Initialize(Transform target, Vector3 offset)
    {
        this.target = target;
        this.offset = offset;
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 forward = transform.position - mainCamera.transform.position;
        if (forward.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(forward.normalized);
        }
    }
}
