using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private playerController player;

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

        float staminaPercent = player.currentStamina / player.maxStamina; // normalized 0-1

        staminaSlider.value = player.currentStamina;

        // Smooth color gradient
        if (staminaPercent > 0.5f)
        {
            fillImage.color = Color.Lerp(halfColor, fullColor, (staminaPercent - 0.5f) * 2f);
        }
        else
        {
            fillImage.color = Color.Lerp(lowColor, halfColor, staminaPercent * 2f);
        }
    }
}