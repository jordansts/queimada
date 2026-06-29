using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ArenaKnockbackMotor : MonoBehaviour
{
    [SerializeField] private float horizontalDamping = 12f;
    [SerializeField] private float verticalDamping = 8.5f;
    [SerializeField] private float maxHorizontalSpeed = 13.5f;
    [SerializeField] private float minVelocity = 0.12f;
    [SerializeField] private float knockbackDuration = 0.24f;

    private CharacterController controller;
    private Vector3 velocity;
    private float knockbackTimer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void AddImpulse(Vector3 impulse)
    {
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(impulse, Vector3.up);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxHorizontalSpeed);

        float verticalVelocity = Mathf.Max(0f, impulse.y);
        velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        knockbackTimer = knockbackDuration;
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
        knockbackTimer = 0f;
    }

    private void LateUpdate()
    {
        if (controller == null || !controller.enabled || velocity.sqrMagnitude < 0.0001f)
        {
            return;
        }

        controller.Move(velocity * Time.deltaTime);
        knockbackTimer -= Time.deltaTime;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, horizontalDamping * Time.deltaTime);

        float verticalVelocity = velocity.y;
        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = 0f;
        }
        else
        {
            verticalVelocity = Mathf.Lerp(verticalVelocity, 0f, verticalDamping * Time.deltaTime);
        }

        velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        if (knockbackTimer <= 0f || velocity.magnitude < minVelocity)
        {
            velocity = Vector3.zero;
            knockbackTimer = 0f;
        }
    }
}
