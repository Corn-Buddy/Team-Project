using UnityEngine;

public class Damage : MonoBehaviour
{
    [SerializeField] int damageAmount;
    [SerializeField] bool destroyOnHit;

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
        {
            return;
        }

        IDamage dmg = other.GetComponent<IDamage>();                         // checks if the object hit can take damage

        if (dmg != null)
        {
            dmg.takeDamage(damageAmount);

            if (destroyOnHit)
            {
                Destroy(gameObject);                                         // useful for projectiles that disappear after hitting
            }
        }
    }
}