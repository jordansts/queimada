using StarterAssets;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ArenaCombatant : MonoBehaviour
{
    private const float BlockDamageMultiplier = 0.35f;
    private const float BlockKnockbackMultiplier = 0.55f;
    private static readonly Vector3 HeldBallLocalPosition = new Vector3(0.01f, -0.02f, -0.02f);

    [SerializeField] private float maxHealth = 100f;

    public string DisplayName { get; private set; }
    public bool IsPlayerControlled { get; private set; }
    public Transform HeldBallAnchor { get; private set; }
    public Transform ThrowOrigin { get; private set; }
    public ArenaCombatant Opponent { get; private set; }
    public Collider[] Colliders { get; private set; }
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

    public void SetAttachmentPoints(Transform heldBallAnchor, Transform throwOrigin)
    {
        HeldBallAnchor = heldBallAnchor;
        ThrowOrigin = throwOrigin;
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
        ResetVerticalVelocity();

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

    private void ResetVerticalVelocity()
    {
        if (thirdPersonController == null)
        {
            return;
        }

        var verticalVelocityField = typeof(ThirdPersonController).GetField("_verticalVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (verticalVelocityField != null)
        {
            verticalVelocityField.SetValue(thirdPersonController, 0f);
        }
    }

    private void EnsureHeldBallVisual()
    {
        if (heldBallVisual != null || HeldBallAnchor == null)
        {
            return;
        }

        if (MiniGameManager.Instance != null)
        {
            heldBallVisual = MiniGameManager.Instance.CreateArenaBallVisualInstance(false, "HeldArenaBall");
        }
        else
        {
            heldBallVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            heldBallVisual.name = "HeldArenaBall";
            heldBallVisual.transform.localScale = GetHeldBallLocalScale();
        }

        heldBallVisual.transform.SetParent(HeldBallAnchor, false);
        heldBallVisual.transform.localPosition = HeldBallLocalPosition;
        heldBallVisual.transform.localRotation = Quaternion.identity;
        heldBallVisual.transform.localScale = GetHeldBallLocalScale();

        Collider collider = heldBallVisual.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void UpdateHeldBallVisual()
    {
        if (heldBallVisual != null)
        {
            heldBallVisual.SetActive(HasBall);
        }
    }

    private Vector3 GetHeldBallLocalScale()
    {
        float scale = MiniGameManager.Instance != null ? MiniGameManager.Instance.BallVisualScale : 0.42f;
        if (HeldBallAnchor == null)
        {
            return Vector3.one * scale;
        }

        Vector3 parentScale = HeldBallAnchor.lossyScale;
        float safeX = Mathf.Abs(parentScale.x) > 0.0001f ? parentScale.x : 1f;
        float safeY = Mathf.Abs(parentScale.y) > 0.0001f ? parentScale.y : 1f;
        float safeZ = Mathf.Abs(parentScale.z) > 0.0001f ? parentScale.z : 1f;
        return new Vector3(scale / safeX, scale / safeY, scale / safeZ);
    }
}
