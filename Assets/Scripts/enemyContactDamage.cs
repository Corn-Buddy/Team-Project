using UnityEngine;
using System.Collections;

public class enemyContactDamage : MonoBehaviour
{
    [SerializeField] int damageAmount;
    [SerializeField] float damageRate;

    bool isDamaging;

    private void OnTriggerStay(Collider other)
    {
        if (isDamaging)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            IDamage dmg = other.GetComponent<IDamage>();                     // checks if the player can take damage

            if (dmg != null)
            {
                StartCoroutine(damageOther(dmg));                            // starts cooldown-based contact damage
            }
        }
    }

    IEnumerator damageOther(IDamage dmg)
    {
        isDamaging = true;                                                   // prevents damage every frame

        dmg.takeDamage(damageAmount);

        yield return new WaitForSeconds(damageRate);                         // waits before allowing damage again

        isDamaging = false;
    }
}