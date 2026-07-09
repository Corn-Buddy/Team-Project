using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class playerHealth : MonoBehaviour, IDamage
{
    [SerializeField] int HP;
    [SerializeField] Image playerHPBar;
    [SerializeField] GameObject playerDamageScreen;

    int HPOrig;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HPOrig = HP;                                                         // saves starting HP for health bar math
        updatePlayerUI();

        if (playerDamageScreen != null)
        {
            playerDamageScreen.SetActive(false);
        }
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        updatePlayerUI();
        StartCoroutine(flashDamage());                                       // gives the player visual feedback when hit

        if (HP <= 0)
        {
            gamemanager.instance.youLose();                                  // calls the lose state when HP reaches zero
        }
    }

    void updatePlayerUI()
    {
        if (playerHPBar != null)
        {
            playerHPBar.fillAmount = Mathf.Clamp01((float)HP / HPOrig);      // converts HP into a 0-1 fill value
        }
    }

    IEnumerator flashDamage()
    {
        if (playerDamageScreen == null)
        {
            yield break;
        }

        playerDamageScreen.SetActive(true);
        yield return new WaitForSeconds(0.1f);                               // keeps the flash visible for a short moment
        playerDamageScreen.SetActive(false);
    }
}