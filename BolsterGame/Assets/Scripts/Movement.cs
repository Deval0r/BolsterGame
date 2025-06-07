using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Springy Side Movement Settings")]
    public float sideMoveRange = 5f;   // Lane boundaries for lateral movement
    public float springStrength = 20f; // Force multiplier for lateral correction
    public float damping = 4f;         // Damping factor

    [Header("Player Offset Settings")]
    public float zOffset = 5f;         // Fixed offset ahead of the camera on the z-axis

    [Header("Flight Mode Settings")]
    public float flightUpForce = 10f;      // Force applied when boosting in flight mode
    public float maxFlightVelocity = 15f;  // Maximum upward velocity in flight mode
    public float flightGravity = 20f;      // Gravity applied in flight mode
    public float normalGravity = 78.4f;    // Normal gravity (8 * 9.8)

    [Header("Explosion Settings")]
    [Tooltip("Prefab for a piece that spawns on explosion. It should have a Rigidbody component.")]
    public GameObject explosionPiecePrefab;
    [Tooltip("Number of pieces spawned on explosion.")]
    public int numExplosionPieces = 10;
    [Tooltip("Force applied to each explosion piece.")]
    public float explosionForce = 300f;
    [Tooltip("Radius of the explosion force.")]
    public float explosionRadius = 5f;
    [Tooltip("Time in seconds each explosion piece remains before being destroyed.")]
    public float pieceLifeTime = 3f;

    [Header("Audio Settings")]
    [Tooltip("Audio clip to play when the player explodes")]
    public AudioClip explosionSound;
    [Tooltip("Volume of the explosion sound (0-1)")]
    public float explosionVolume = 1f;

    private float targetX = 0f;   // Desired lane x-position based on input
    private float velocityX = 0f; // Lateral velocity computed via spring-damper
    private Rigidbody rb;
    private bool exploded = false;
    private bool isInFlightMode = false;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
        Physics.gravity = new Vector3(0, -normalGravity, 0);
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        targetX = transform.position.x;
    }

    private void Update()
    {
        if (exploded)
            return; // Stop processing input once exploded

        // Process horizontal input using a three-lane system.
        float input = Input.GetAxisRaw("Horizontal");
        if (input < -0.1f)
            targetX = -sideMoveRange; // Left lane
        else if (input > 0.1f)
            targetX = sideMoveRange;  // Right lane
        else
            targetX = 0f;             // Center lane

        // Handle flight mode boost
        if (isInFlightMode && Input.GetKey(KeyCode.W))
        {
            // Apply upward force when boosting in flight mode
            rb.AddForce(Vector3.up * flightUpForce, ForceMode.Force);
            
            // Clamp the upward velocity
            Vector3 velocity = rb.linearVelocity;
            velocity.y = Mathf.Min(velocity.y, maxFlightVelocity);
            rb.linearVelocity = velocity;
        }
    }

    private void FixedUpdate()
    {
        if (exploded)
            return; // Disable movement after explosion

        // Compute spring-damper dynamics for smooth horizontal movement.
        float currentX = transform.position.x;
        float displacement = targetX - currentX;
        float springForce = displacement * springStrength;
        float dampingForce = velocityX * damping;
        float netForce = springForce - dampingForce;
        velocityX += netForce * Time.fixedDeltaTime;

        // Snap into lane if already nearly aligned to avoid jitter.
        if (Mathf.Abs(displacement) < 0.01f && Mathf.Abs(velocityX) < 0.01f)
        {
            currentX = targetX;
            velocityX = 0f;
        }

        // Use the forward speed from the camera controller, if available.
        float forwardSpeed = (CameraSpeedController.Instance != null)
                                 ? CameraSpeedController.Instance.CurrentSpeed
                                 : 0f;

        Vector3 newVelocity = new Vector3(velocityX, rb.linearVelocity.y, forwardSpeed);
        rb.linearVelocity = newVelocity;

        // Apply flight mode gravity
        if (isInFlightMode)
        {
            rb.AddForce(Vector3.down * flightGravity, ForceMode.Force);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if we entered a flight mode trigger
        if (other.CompareTag("FlightTrigger"))
        {
            isInFlightMode = true;
            // Disable normal gravity and enable flight mode gravity
            Physics.gravity = Vector3.zero;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if we exited a flight mode trigger
        if (other.CompareTag("FlightTrigger"))
        {
            isInFlightMode = false;
            // Restore normal gravity
            Physics.gravity = new Vector3(0, -normalGravity, 0);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If the player collides with a spike (tagged "Spike") and hasn't exploded yet, trigger the explosion.
        if (!exploded && collision.gameObject.CompareTag("Spike"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        exploded = true;

        // Play explosion sound from camera's audio source
        if (explosionSound != null && CameraSpeedController.Instance != null)
        {
            AudioSource cameraAudio = CameraSpeedController.Instance.GetComponent<AudioSource>();
            if (cameraAudio != null)
            {
                cameraAudio.PlayOneShot(explosionSound, explosionVolume);
            }
        }

        // Optionally, disable the player's visual representation.
        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (mesh != null)
        {
            mesh.enabled = false;
        }

        // Also disable the player's collider to prevent further collisions.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Stop all movement.
        rb.linearVelocity = Vector3.zero;

        // Spawn explosion pieces.
        for (int i = 0; i < numExplosionPieces; i++)
        {
            // Slightly randomize the spawn position within a small sphere.
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 0.5f;
            GameObject piece = Instantiate(explosionPiecePrefab, spawnPos, Random.rotation);

            // Apply explosion force, if the piece has a Rigidbody.
            Rigidbody pieceRb = piece.GetComponent<Rigidbody>();
            if (pieceRb != null)
            {
                pieceRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            // Automatically destroy the piece after a set time.
            Destroy(piece, pieceLifeTime);
        }

        // Destroy the player object after a short delay.
        Destroy(gameObject, 0.1f);
    }
}
