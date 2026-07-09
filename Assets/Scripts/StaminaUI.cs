using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaminaUI : MonoBehaviour
{
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private playerController player;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("Color Gradient")]
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color halfColor = Color.yellow;
    [SerializeField] private Color lowColor = Color.red;

    private Image fillImage;
    void Start()
    {
        if (staminaSlider != null)
        {
            fillImage = staminaSlider.fillRect.GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (player == null || staminaSlider == null) return;

        float current = player.currentStamina;
        float max = player.maxStamina;

        staminaSlider.value = current;

        // Update Text
        if (staminaText != null)
            staminaText.text = "Stamina: " + $"{Mathf.Ceil(current)} / {max}";

        // Color
        float percent = current / max;
        if (percent > 0.5f)
            fillImage.color = Color.Lerp(halfColor, fullColor, (percent - 0.5f) * 2f);
        else
            fillImage.color = Color.Lerp(lowColor, halfColor, percent * 2f);
    }
}