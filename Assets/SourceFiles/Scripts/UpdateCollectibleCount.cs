using UnityEngine;
using TMPro;

public class UpdateCollectibleCount : MonoBehaviour
{
    private TextMeshProUGUI collectibleText;

    void Start()
    {
        collectibleText = GetComponent<TextMeshProUGUI>();
        if (collectibleText == null)
        {
            Debug.LogError("UpdateCollectibleCount script requires a TextMeshProUGUI component on the same GameObject.");
            return;
        }

        UpdateCollectibleDisplay();
    }

    void Update()
    {
        UpdateCollectibleDisplay();
    }

    private void UpdateCollectibleDisplay()
    {
        if (collectibleText == null)
        {
            return;
        }

        collectibleText.text = MiniGameManager.Instance != null
            ? MiniGameManager.Instance.GetHudText()
            : "Loading arena...";
    }
}
