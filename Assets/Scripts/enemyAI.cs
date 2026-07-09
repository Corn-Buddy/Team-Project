using UnityEngine;
using System.Collections;

public class enemyAI : MonoBehaviour, IDamage
{
    [SerializeField] Rigidbody rb;
    [SerializeField] Renderer model;

    [SerializeField] int HP;
    [SerializeField] float speed;
    [SerializeField] float hitFlashTime = 0.1f;

    Color colorOrig; 
    Transform player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (model != null)
        {
            colorOrig = model.material.color;                                // stores original color so the enemy can reset after flashing
        }

        if (gamemanager.instance != null && gamemanager.instance.player != null)
        {
            player = gamemanager.instance.player.transform;                  // stores the player as the enemy target
        }
    }

    void FixedUpdate()
    {
        if (gamemanager.instance != null && gamemanager.instance.isGameOver)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        followPlayer();
    }

    void followPlayer()
    {
        Vector3 direction = player.position - transform.position;            // gets direction from enemy to player
        direction.y = 0;                                                     // keeps enemy movement flat on the ground
        direction = direction.normalized;                                    // keeps movement speed consistent

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    public void takeDamage(int amount)
    {
        HP -= amount;

        if (HP <= 0)
        {
            die();
        }
        else
        {
            StartCoroutine(flashRed());                                      // shows hit feedback if the enemy survives
        }
    }

    void die()
    {
        if (gamemanager.instance != null)
        {
            gamemanager.instance.addKill();                                  // tells the manager an enemy was defeated
        }

        Destroy(gameObject);
    }

    IEnumerator flashRed()
    {
        if (model == null)
        {
            yield break;
        }

        model.material.color = Color.red;
        yield return new WaitForSeconds(hitFlashTime);
        model.material.color = colorOrig;                                    // returns enemy to its original color
    }
}