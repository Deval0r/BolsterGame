using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Springy Side Movement Settings")]
    public float sideMoveRange = 5f;   // Lane boundaries for lateral movement
    public float springStrength = 20f; // Force multiplier for lateral correction
    public float damping = 4f;         // Damping factor

    private float targetX = 0f;        // Desired lane x-position
    private float velocityX = 0f;      // Lateral computed velocity from spring-damper
    private Rigidbody rb;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // DOUBLE THE GRAVITY: note this applies globally!
        Physics.gravity *= 8f;

        // Set initial target to current x position.
        targetX = transform.position.x;
    }

    private void Update()
    {
        // Process horizontal input via a three-lane system.
        float input = Input.GetAxisRaw("Horizontal");
        if (input < -0.1f)
            targetX = -sideMoveRange;  // Left lane
        else if (input > 0.1f)
            targetX = sideMoveRange;   // Right lane
        else
            targetX = 0f;              // Center lane
    }

    private void FixedUpdate()
    {
        // Compute spring-damper dynamics on the x-axis.
        float currentX = transform.position.x;
        float displacement = targetX - currentX;
        float springForce = displacement * springStrength;
        float dampingForce = velocityX * damping;
        float netForce = springForce - dampingForce;
        velocityX += netForce * Time.fixedDeltaTime;

        // Snap horizontally if nearly aligned.
        if (Mathf.Abs(displacement) < 0.01f && Mathf.Abs(velocityX) < 0.01f)
        {
            currentX = targetX;
            velocityX = 0f;
        }

        // Retrieve the forward speed from the camera controller.
        float forwardSpeed = (CameraSpeedController.Instance != null)
                                 ? CameraSpeedController.Instance.CurrentSpeed
                                 : 0f;

        // Combine computed x velocity with preserved y (gravity will now be stronger) and forward z component.
        Vector3 newVelocity = new Vector3(velocityX, rb.linearVelocity.y, forwardSpeed);
        rb.linearVelocity = newVelocity;
    }
}
