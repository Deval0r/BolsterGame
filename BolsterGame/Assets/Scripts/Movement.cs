using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Springy Side Movement Settings")]
    public float sideMoveRange = 5f; // How far left/right the player can move
    public float springStrength = 20f; // Spring force
    public float damping = 4f; // Damping force
    public float sideInputSpeed = 10f; // How quickly targetX changes with input

    [Header("Player Offset Settings")]
    public float zOffset = 5f; // Player stays this far ahead of the camera

    private float targetX = 0f;
    private float velocityX = 0f;
    private Transform camTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;
        camTransform = Camera.main.transform;
        targetX = transform.position.x;
    }

    // Update is called once per frame
    void Update()
    {
        // Three-lane input system
        float input = Input.GetAxisRaw("Horizontal");
        if (input < -0.1f)
            targetX = -sideMoveRange; // Left
        else if (input > 0.1f)
            targetX = sideMoveRange; // Right
        else
            targetX = 0f; // Center

        // Spring-damper system for x movement
        float displacement = targetX - transform.position.x;
        float springForce = displacement * springStrength;
        float damper = velocityX * damping;
        float force = springForce - damper;
        velocityX += force * Time.deltaTime;
        transform.position += new Vector3(velocityX * Time.deltaTime, 0, 0);

        // Stop jitter: snap to target if close enough
        if (Mathf.Abs(displacement) < 0.01f && Mathf.Abs(velocityX) < 0.01f)
        {
            Vector3 pos = transform.position;
            pos.x = targetX;
            transform.position = pos;
            velocityX = 0f;
        }

        // Follow camera's z position with offset
        if (camTransform != null)
        {
            Vector3 pos = transform.position;
            pos.z = camTransform.position.z + zOffset;
            transform.position = pos;
        }
    }
}
