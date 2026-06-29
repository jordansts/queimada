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
    public Transform WeaponMuzzle { get; private set; }
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

    public void SetWeaponMuzzle(Transform muzzle)
    {
        WeaponMuzzle = muzzle;
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
        if (heldBallVisual != null || WeaponMuzzle == null)
        {
            return;
        }

        heldBallVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        heldBallVisual.name = "HeldArenaBall";
        heldBallVisual.transform.SetParent(WeaponMuzzle, false);
        heldBallVisual.transform.localPosition = new Vector3(0f, 0f, -0.08f);
        heldBallVisual.transform.localRotation = Quaternion.identity;
        heldBallVisual.transform.localScale = Vector3.one * 0.32f;

        Collider collider = heldBallVisual.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = heldBallVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = IsPlayerControlled ? new Color(0.25f, 0.9f, 1f) : new Color(1f, 0.4f, 0.2f);
        }
    }

    private void UpdateHeldBallVisual()
    {
        if (heldBallVisual != null)
        {
            heldBallVisual.SetActive(HasBall);
        }
    }
}
