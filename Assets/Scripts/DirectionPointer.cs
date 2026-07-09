using UnityEngine;

public class DirectionPointer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float distanceInFront = 1.2f;   // How far ahead the pointer sits
    [SerializeField] private float heightAbove = 1.8f;       // How high it floats

    private Transform player;
    void Start()
    {
        player = transform.parent;  
    }
    void Update()
    {
        if (player == null) {
            return;
        }

        Vector3 forwardDir = player.forward;
        Vector3 targetPos = player.position + forwardDir * distanceInFront;

        targetPos.y = player.position.y + heightAbove;

        transform.position = targetPos;

        transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
    }
}
