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

    public void Initialize(Vector3 groundedPosition, float pickupDelay, bool useFloatingMotion = true)
    {
        startPosition = groundedPosition;
        transform.position = groundedPosition;
        pickupDelayRemaining = pickupDelay;
        this.useFloatingMotion = useFloatingMotion;
        attachedRigidbody = GetComponent<Rigidbody>();
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
        Collider[] overlaps = Physics.OverlapSphere(transform.position, claimRadius, ~0, QueryTriggerInteraction.Ignore);
        ArenaCombatant bestCombatant = null;
        float bestDistance = float.MaxValue;

        foreach (Collider overlap in overlaps)
        {
            ArenaCombatant combatant = overlap.GetComponentInParent<ArenaCombatant>();
            if (combatant == null || combatant.HasBall)
            {
                continue;
            }

            float distance = Vector3.Distance(
                Vector3.ProjectOnPlane(combatant.transform.position, Vector3.up),
                Vector3.ProjectOnPlane(transform.position, Vector3.up));

            if (bestCombatant == null || distance < bestDistance - 0.01f)
            {
                bestCombatant = combatant;
                bestDistance = distance;
                continue;
            }

            if (Mathf.Abs(distance - bestDistance) <= 0.01f && combatant.IsPlayerControlled && !bestCombatant.IsPlayerControlled)
            {
                bestCombatant = combatant;
                bestDistance = distance;
            }
        }

        if (bestCombatant == null)
        {
            return;
        }

        claimed = true;
        MiniGameManager.Instance?.ClaimArenaBall(this, bestCombatant);
    }
}
