using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ArenaBallPickup : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float bobbingAmount = 0.12f;
    [SerializeField] private float bobbingSpeed = 1.8f;
    [SerializeField] private float claimRadius = 1.15f;

    private Vector3 startPosition;
    private float timer;
    private bool claimed;
    private float pickupDelayRemaining;
    private bool useFloatingMotion = true;
    private Rigidbody attachedRigidbody;

    public void Initialize(Vector3 groundedPosition, float pickupDelay, bool useFloatingMotion = true, bool useTriggerCollider = true)
    {
        claimed = false;
        timer = 0f;
        startPosition = groundedPosition;
        transform.position = groundedPosition;
        pickupDelayRemaining = pickupDelay;
        this.useFloatingMotion = useFloatingMotion;
        attachedRigidbody = GetComponent<Rigidbody>();

        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = useTriggerCollider;
        }

        if (attachedRigidbody != null)
        {
            if (useFloatingMotion)
            {
                attachedRigidbody.isKinematic = false;
                attachedRigidbody.useGravity = false;
                attachedRigidbody.linearVelocity = Vector3.zero;
                attachedRigidbody.angularVelocity = Vector3.zero;
            }

            attachedRigidbody.useGravity = !useFloatingMotion;
            attachedRigidbody.isKinematic = useFloatingMotion;
        }
    }

    private void Start()
    {
        if (attachedRigidbody == null)
        {
            attachedRigidbody = GetComponent<Rigidbody>();
        }

        if (startPosition == Vector3.zero)
        {
            startPosition = transform.position;
        }
    }

    private void Update()
    {
        if (pickupDelayRemaining > 0f)
        {
            pickupDelayRemaining -= Time.deltaTime;
        }

        if (useFloatingMotion)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            timer += Time.deltaTime * bobbingSpeed;
            float newY = startPosition.y + Mathf.Sin(timer) * bobbingAmount;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
        else
        {
            startPosition = transform.position;
        }

        if (!claimed && pickupDelayRemaining <= 0f)
        {
            TryClaimNearbyCombatant();
        }
    }

    private void TryClaimNearbyCombatant()
    {
        ArenaCombatant bestCombatant = MiniGameManager.Instance != null
            ? MiniGameManager.Instance.FindBestBallClaimant(transform.position, claimRadius)
            : null;

        if (bestCombatant == null)
        {
            return;
        }

        claimed = true;
        MiniGameManager.Instance?.ClaimArenaBall(this, bestCombatant);
    }
}
