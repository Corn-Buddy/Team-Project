using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTrail : MonoBehaviour
{
    [SerializeField] private TrailRenderer trail;

    private playerController player;

    private void Start()
    {
        player = GetComponent<playerController>();
        if (trail == null)
            trail = GetComponentInChildren<TrailRenderer>(); // safer
    }

    private void Update()
    {
        if (trail == null || player == null) return;

        bool shouldEmit = player.isDashing ||
                          InputSystem.actions.FindAction("Sprint").IsPressed();

        trail.emitting = shouldEmit;
    }
}