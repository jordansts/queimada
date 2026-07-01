using StarterAssets;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ArenaCombatant : MonoBehaviour
{
    private const float BlockDamageMultiplier = 0.35f;
    private const float BlockKnockbackMultiplier = 0.55f;

    [SerializeField] private float maxHealth = 100f;

    public string DisplayName { get; private set; }
    public bool IsPlayerControlled { get; private set; }
    public Transform HeldBallAnchor { get; private set; }
    public Transform HeldBallVisualTransform => heldBallVisual != null ? heldBallVisual.transform : null;
    public Transform ThrowOrigin { get; private set; }
    public ArenaCombatant Opponent { get; private set; }
    public Collider[] Colliders { get; private set; }
    public ThirdPersonController Controller => thirdPersonController;
    public bool HasBall { get; private set; }
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0f ? CurrentHealth / maxHealth : 0f;
    public bool IsBlocking => thirdPersonController != null && thirdPersonController.IsBlocking;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private ArenaKnockbackMotor knockbackMotor;
    private CharacterController characterController;
    private ThirdPersonController thirdPersonController;
    private GameObject heldBallVisual;

    public void Initialize(string displayName, bool isPlayerControlled, Vector3 spawnPosition, Quaternion spawnRotation, ArenaKnockbackMotor knockbackMotor)
    {
        DisplayName = displayName;
        IsPlayerControlled = isPlayerControlled;
        this.spawnPosition = spawnPosition;
        this.spawnRotation = spawnRotation;
        this.knockbackMotor = knockbackMotor;
        characterController = GetComponent<CharacterController>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        Colliders = GetComponentsInChildren<Collider>(true);
        CurrentHealth = maxHealth;
    }

    public void SetOpponent(ArenaCombatant opponent)
    {
        Opponent = opponent;
    }

    public void SetAttachmentPoints(Transform heldBallAnchor, Transform throwOrigin, GameObject existingHeldBallVisual)
    {
        HeldBallAnchor = heldBallAnchor;
        ThrowOrigin = throwOrigin;
        heldBallVisual = existingHeldBallVisual;
        EnsureHeldBallVisual();
        UpdateHeldBallVisual();
    }

    public void GiveBall()
    {
        HasBall = true;
        EnsureHeldBallVisual();
        UpdateHeldBallVisual();
    }

    public void RemoveBall()
    {
        HasBall = false;
        UpdateHeldBallVisual();
    }

    public void ApplyHit(float damage, Vector3 impulse)
    {
        float appliedDamage = damage;
        Vector3 appliedImpulse = impulse;

        if (IsBlocking)
        {
            appliedDamage *= BlockDamageMultiplier;
            appliedImpulse *= BlockKnockbackMultiplier;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - appliedDamage);
        knockbackMotor?.AddImpulse(appliedImpulse);

        if (CurrentHealth <= 0f)
        {
            MiniGameManager.Instance?.HandleDefeat(this);
        }
    }

    public void Respawn()
    {
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        knockbackMotor?.ResetVelocity();
        thirdPersonController?.ResetMotionState();

        if (thirdPersonController != null)
        {
            thirdPersonController.ResetCameraRotation(spawnRotation.eulerAngles.y);
        }

        RestoreFullHealth();
        UpdateHeldBallVisual();
    }

    public void RestoreFullHealth()
    {
        CurrentHealth = maxHealth;
    }

    public Vector3 GetBallReleaseOrigin()
    {
        if (ThrowOrigin != null)
        {
            return ThrowOrigin.position;
        }

        if (heldBallVisual != null)
        {
            return heldBallVisual.transform.position;
        }

        if (HeldBallAnchor != null)
        {
            return HeldBallAnchor.position;
        }

        return transform.position;
    }

    private void EnsureHeldBallVisual()
    {
        if (heldBallVisual != null || HeldBallAnchor == null)
        {
            return;
        }

        Transform existingHeldBall = HeldBallAnchor.Find("HeldArenaBall");
        heldBallVisual = existingHeldBall != null ? existingHeldBall.gameObject : null;
        if (heldBallVisual == null)
        {
            Debug.LogError($"ArenaCombatant on '{name}' is missing HeldArenaBall under HeldBallAnchor.", this);
            return;
        }

        heldBallVisual.transform.SetParent(HeldBallAnchor, false);
        heldBallVisual.transform.localRotation = Quaternion.identity;
        ApplyHeldBallTransform();
    }

    private void UpdateHeldBallVisual()
    {
        if (heldBallVisual != null)
        {
            ApplyHeldBallTransform();
            heldBallVisual.SetActive(HasBall);
        }
    }

    private void ApplyHeldBallTransform()
    {
        if (heldBallVisual == null || HeldBallAnchor == null)
        {
            return;
        }

        heldBallVisual.transform.localRotation = Quaternion.identity;
    }
}
