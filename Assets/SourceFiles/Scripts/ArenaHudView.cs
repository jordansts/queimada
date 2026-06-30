using UnityEngine;
using UnityEngine.UI;

public class ArenaHudView : MonoBehaviour
{
    [SerializeField] private Text controlsLabel;
    [SerializeField] private Text ballStatusLabel;
    [SerializeField] private Text playerHealthLabel;
    [SerializeField] private Text botHealthLabel;
    [SerializeField] private Image playerHealthFill;
    [SerializeField] private Image botHealthFill;
    [SerializeField] private Image crosshairImage;

    public void Configure(Color playerColor, Color botColor, Color crosshairColor)
    {
        if (playerHealthFill != null)
        {
            playerHealthFill.color = playerColor;
        }

        if (botHealthFill != null)
        {
            botHealthFill.color = botColor;
        }

        if (crosshairImage != null)
        {
            crosshairImage.color = crosshairColor;
        }
    }

    public void Refresh(ArenaCombatant playerCombatant, ArenaCombatant botCombatant, Transform looseBallTransform)
    {
        if (controlsLabel != null)
        {
            controlsLabel.text = "F throw   RMB defend   Ctrl roll   Space double jump   F2 enemy toggle   F3 recover ball";
        }

        if (ballStatusLabel != null)
        {
            ballStatusLabel.text = ResolveBallStatus(playerCombatant, botCombatant, looseBallTransform);
        }

        RefreshHealthBar(playerCombatant, playerHealthFill, playerHealthLabel, "Player");
        RefreshHealthBar(botCombatant, botHealthFill, botHealthLabel, "AI");
    }

    private static void RefreshHealthBar(ArenaCombatant combatant, Image fillImage, Text label, string labelPrefix)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = combatant != null ? combatant.HealthNormalized : 0f;
        }

        if (label == null)
        {
            return;
        }

        if (combatant == null)
        {
            label.text = $"{labelPrefix} HP --/--";
            return;
        }

        label.text = $"{labelPrefix} HP {Mathf.CeilToInt(combatant.CurrentHealth)}/{Mathf.CeilToInt(combatant.MaxHealth)}";
    }

    private static string ResolveBallStatus(ArenaCombatant playerCombatant, ArenaCombatant botCombatant, Transform looseBallTransform)
    {
        if (playerCombatant != null && playerCombatant.HasBall)
        {
            return "Ball: player";
        }

        if (botCombatant != null && botCombatant.HasBall)
        {
            return "Ball: AI";
        }

        return looseBallTransform != null ? "Ball: floor" : "Ball: in play";
    }
}
