using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.VisualScripting;

public class HealthUI : MonoBehaviour, IDamage
{
    [Header("Health Settings")]
    [SerializeField] int maxHP = 100;
    private int currentHP;

    [Header("UI References")]
    [SerializeField] Image healthFillImage;
    [SerializeField] TextMeshProUGUI healthText;

    [Header("Damage Feedback")]
    [SerializeField] private GameObject dmgobj;

    void Start()
    {
        currentHP = maxHP;
        UpdateHealthUI();

        if (dmgobj != null)
        {

            dmgobj.SetActive(false);
        }
    }

    public void takeDamage(int amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Max(0, currentHP);

        UpdateHealthUI();
        StartCoroutine(flashRed());

        if (currentHP <= 0)
        {
            gamemanager.instance.youLose();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = (float)currentHP / maxHP;
        }

        if (healthText != null)
        {
            healthText.text = "Health: " + $"{currentHP} / {maxHP}";
        }
    }

    private IEnumerator flashRed()
    {
        if (dmgobj == null) yield break;
        {
            dmgobj.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            dmgobj.SetActive(false);
        }
    }
}