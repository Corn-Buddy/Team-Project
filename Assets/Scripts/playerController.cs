using UnityEngine;
using UnityEngine.InputSystem;

public class playerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CharacterController controller;

    [Header("Movement")]
    [SerializeField] float speed = 8f; // stores the player's movement speed
    [SerializeField] float sprintMod = 1.8f; // stores the multiplier for the player's sprint speed

    [Header("Jump")]
    [SerializeField] float jumpSpeed = 3f; // stores the speed at which the player jumps
    [SerializeField] int jumpMax = 2; // stores the maximum number of jumps the player can make
    [SerializeField] float gravity = 9.81f; // stores the gravity value for the player

    [Header("Combat")]
    [SerializeField] int HP = 100; // stores the player's current health points
    [SerializeField] int shootDamage = 25; // stores the amount of damage the player does per shot
    [SerializeField] float shootRate = 0.25f; // stores the time between shots
    [SerializeField] float shootDist = 100f; // stores the maximum distance the player can shoot
    [SerializeField] LayerMask ignoreLayer;

    Vector3 playerVel; // stores the player's current velocity
    int jumpCount; // stores the number of jumps the player has made
    float shootTimer; // stores the time since the last shot was fired
    Vector3 moveDir; // stores the direction the player is moving in
    int HPOrig; // stores the original HP value for reference

    private float currentSpeed;  // Used internally for sprinting

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        HPOrig = HP;
        currentSpeed = speed;
    }

    void Update()
    {
        movement();
        sprint();
    }

    void movement()
    {
        if (controller.isGrounded)
        {
            playerVel.y = 0;
            jumpCount = 0;
        }

        // New Input System - WASD movement
        Vector2 input = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
        moveDir = transform.right * input.x + transform.forward * input.y;

        controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);

        jump();

        controller.Move(playerVel * Time.deltaTime);
        playerVel.y -= gravity * Time.deltaTime;

        // Shooting
        shootTimer += Time.deltaTime;
        if (InputSystem.actions.FindAction("Fire").IsPressed() && shootTimer > shootRate)
        {
            shoot();
        }

        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);
    }

    void sprint()
    {
        // Simple sprint with Left Shift (you can change to a proper Input Action later)
        if (InputSystem.actions.FindAction("Sprint").IsPressed())
        {
            currentSpeed = speed * sprintMod;
        }
        else
        {
            currentSpeed = speed;
        }
    }

    void jump()
    {
        if (InputSystem.actions.FindAction("Jump").triggered && jumpCount < jumpMax)
        {
            playerVel.y = jumpSpeed;   // Note: You may want to increase this value (try 12-18)
            jumpCount++;
        }
    }

    void shoot()
    {
        shootTimer = 0;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, shootDist, ~ignoreLayer))
        {
            Debug.Log(hit.collider.name);

            IDamage dmg = hit.collider.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(shootDamage);
            }
        }
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        if (HP <= 0)
        {
            // gamemanager.instance.youLose();
            Debug.Log("Player Died!");
        }
    }
}















    //void Start()
    //{
    //    HPOrig = HP; 
    //}

    //void Update()
    //{
    //    movement();

    //    sprint();
    //}

    //void movement()
    //{
    //    if (controller.isGrounded)
    //    {
    //        playerVel.y = 0;
    //        jumpCount = 0;
    //    }

    //    moveDir = Input.GetAxis("Horizontal") * transform.right + Input.GetAxis("Vertical") * transform.forward;
    //    controller.Move(moveDir.normalized * speed * Time.deltaTime);

    //    jump();

    //    controller.Move(playerVel * Time.deltaTime);
    //    playerVel.y -= gravity * Time.deltaTime;

    //    shootTimer += Time.deltaTime;
    //    if (Input.GetButton("Fire1") && shootTimer > shootRate)
    //    {

    //        shoot();
    //    }

    //    Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);

    //}

    //void sprint()
    //{
    //    if (Input.GetButtonDown("Sprint"))
    //    {
    //        speed *= sprintMod;
    //    }
    //    else if (Input.GetButtonUp("Sprint"))
    //    {
    //        speed /= sprintMod;
    //    }
    //}

    //void jump()
    //{
    //    if (Input.GetButtonDown("Jump") && jumpCount < jumpMax)
    //    {
    //        playerVel.y = jumpSpeed;
    //        jumpCount++;
    //    }
    //}

    //void shoot()
    //{
    //    shootTimer = 0;

    //    RaycastHit hit;

    //    if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, shootDist, ~ignoreLayer))
    //    {

    //        Debug.Log(hit.collider.name);

    //        IDamage dmg = hit.collider.GetComponent<IDamage>();

    //        if (dmg != null)
    //        {
    //            dmg.takeDamage(shootDamage);
    //        }
    //    }
    //}

    //public void takeDamage(int amount)
    //{
    //    HP -= amount;
    //    if (HP <= 0)
    //    {
    //        gamemanager.instance.youLose();

    //    }
    //}


