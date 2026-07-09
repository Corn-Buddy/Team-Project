using UnityEngine;
using UnityEngine.InputSystem;   // ← Add this

public class cameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;

    [Header("Camera Settings")]
    [SerializeField] float smoothSpeed = 6f;
    [SerializeField] Vector3 offset = new Vector3(0, 18f, -10f);

    [Header("Dynamic Movement")]
    [SerializeField] float sprintLagAmount = 3f;
    [SerializeField] float dashLagAmount = 6f;
    [SerializeField] float lagResetSpeed = 4f;

    private Vector3 currentOffset;
    private float currentLag;

    private void Start()
    {
        currentOffset = offset;
        currentLag = 0f;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        playerController player = target.GetComponent<playerController>();

        bool isSprinting = InputSystem.actions.FindAction("Sprint")?.IsPressed() == true;
        bool isDashing = player != null && player.isDashing;

        float targetLag = 0f;
        if (isDashing) targetLag = dashLagAmount;
        else if (isSprinting) targetLag = sprintLagAmount;

        currentLag = Mathf.Lerp(currentLag, targetLag, lagResetSpeed * Time.deltaTime);

        Vector3 lagOffset = target.forward * currentLag;

        Vector3 desiredPosition = target.position + offset + lagOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
        transform.LookAt(target.position + Vector3.up * 3f);
    }
}