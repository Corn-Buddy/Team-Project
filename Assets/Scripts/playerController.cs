using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class playerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CharacterController controller;

    [Header("Movement")]
    [SerializeField] float speed = 8f;
    [SerializeField] float sprintMod = 1.8f;

    [Header("Jump")]
    [SerializeField] float jumpSpeed = 15f;        // Increased - 3 was too weak
    [SerializeField] int jumpMax = 2;
    [SerializeField] float gravity = 25f;          // Stronger gravity feels better

    [Header("Combat")]
    [SerializeField] int HP = 100;
    [SerializeField] int shootDamage = 25;
    [SerializeField] float shootRate = 0.25f;
    [SerializeField] float shootDist = 100f;
    [SerializeField] LayerMask ignoreLayer;

    [Header("Stamina & Sprinting")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float staminaRecoveryRate = 15f;
    [SerializeField] float sprintStaminaCost = 20f;

    [Header("Dash")]
    [SerializeField] float dashSpeed = 25f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashStaminaCost = 35f;
    [SerializeField] float dashCooldown = 1.2f;

    Vector3 playerVel;
    int jumpCount;
    float shootTimer;
    Vector3 moveDir;
    int HPOrig;

    private float currentSpeed;
    private float currentStamina;
    private float dashTimer;
    private float dashCooldownTimer;
    public bool isDashing { get; private set; }
    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        HPOrig = HP;
        currentSpeed = speed;
        currentStamina = maxStamina;
    }

    void Update()
    {
        movement();
        sprint();
        dash();
        staminaManagement();
        handleDash();
        rotation();

        // Debug (only when pressed, not every frame)
        if (InputSystem.actions.FindAction("Sprint")?.IsPressed() == true)
            Debug.Log("Sprint Held");
        if (InputSystem.actions.FindAction("Dash")?.triggered == true)
            Debug.Log("DASH!");
    }

    void movement()
    {
        if (controller.isGrounded)
        {
            playerVel.y = 0f;
            jumpCount = 0;
        }

        Vector2 input = InputSystem.actions.FindAction("Move")?.ReadValue<Vector2>() ?? Vector2.zero;
        moveDir = transform.right * input.x + transform.forward * input.y;

        controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);

        jump();

        controller.Move(playerVel * Time.deltaTime);
        playerVel.y -= gravity * Time.deltaTime;

        // Shooting
        shootTimer += Time.deltaTime;
        if (InputSystem.actions.FindAction("Fire")?.IsPressed() == true && shootTimer > shootRate)
        {
            shoot();
        }

        if (Camera.main != null)
        {
            Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);
        }
    }

    void jump()
    {
        if (InputSystem.actions.FindAction("Jump")?.triggered == true && jumpCount < jumpMax)
        {
            playerVel.y = jumpSpeed;
            jumpCount++;
        }
    }

    void shoot()
    {
        shootTimer = 0;
        if (Camera.main == null) return;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, shootDist, ~ignoreLayer))
        {
            Debug.Log("Hit: " + hit.collider.name);
            IDamage dmg = hit.collider.GetComponent<IDamage>();
            if (dmg != null)
                dmg.takeDamage(shootDamage);
        }
    }

    void staminaManagement()
    {
        if (!isDashing && InputSystem.actions.FindAction("Sprint")?.IsPressed() != true)
        {
            currentStamina = Mathf.Min(currentStamina + staminaRecoveryRate * Time.deltaTime, maxStamina);
        }
        currentStamina = Mathf.Max(currentStamina, 0f);
    }

    void sprint()
    {
        if (InputSystem.actions.FindAction("Sprint")?.IsPressed() == true && currentStamina > 5f && !isDashing)
        {
            currentSpeed = speed * sprintMod;
            currentStamina -= sprintStaminaCost * Time.deltaTime;
        }
        else
        {
            currentSpeed = speed;
        }
    }

    void dash()
    {
        if (InputSystem.actions.FindAction("Dash")?.triggered == true &&
            currentStamina >= dashStaminaCost &&
            dashCooldownTimer <= 0 &&
            !isDashing)
        {
            isDashing = true;
            currentStamina -= dashStaminaCost;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            Debug.Log("DASH ACTIVATED!");
        }
    }

    void handleDash()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            Vector2 input = InputSystem.actions.FindAction("Move")?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector3 dashDirection = transform.right * input.x + transform.forward * input.y;

            if (dashDirection.magnitude < 0.1f)
                dashDirection = transform.forward;

            controller.Move(dashDirection.normalized * dashSpeed * Time.deltaTime);

            if (dashTimer <= 0f)
                isDashing = false;
        }

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        if (HP <= 0)
        {
            Debug.Log("Player Died!");
        }
    }
    void rotation()
{
    Vector2 input = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();

    if (input.magnitude > 0.1f)
    {
        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        
        // Smoother & slower rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
    }
}
}