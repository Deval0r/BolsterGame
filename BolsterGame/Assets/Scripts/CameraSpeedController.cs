using UnityEngine;

public class CameraSpeedController : MonoBehaviour
{
    public static CameraSpeedController Instance { get; private set; }

    [Header("Camera Speed Settings")]
    public float maxSpeed = 10f;
    public float acceleration = 5f; // How quickly the forward/global speed increases

    [Header("Camera Follow Settings")]
    public float followSpeed = 5f;  // Smoothing factor for following the player's position
    public float zOffset = 10f;     // Offset on the z-axis relative to the player

    [Header("Camera Rotation Settings")]
    public float rotationSmoothSpeed = 5f; // Smoothing factor for rotation changes
    public float cameraPitchOffset = -45f; // Fixed pitch (x-axis rotation) offset
    public float maxRollAngle = 45f;        // Maximum roll angle (z-axis rotation) when player is at lane extremes

    public float CurrentSpeed { get; private set; } = 0f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        // Update the global forward speed.
        CurrentSpeed += acceleration * (1f - (CurrentSpeed / maxSpeed)) * Time.deltaTime;
        CurrentSpeed = Mathf.Clamp(CurrentSpeed, 0f, maxSpeed);

        if (PlayerMovement.Instance != null)
        {
            // Smoothly follow the player's x and z position.
            Vector3 targetPos = new Vector3(
                PlayerMovement.Instance.transform.position.x,
                transform.position.y,
                PlayerMovement.Instance.transform.position.z + zOffset
            );
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

            // Determine the desired yaw to face the player.
            Vector3 toPlayer = PlayerMovement.Instance.transform.position - transform.position;
            Vector3 horizontalToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
            float desiredYaw = 0f;
            if (horizontalToPlayer.sqrMagnitude > 0.001f)
            {
                desiredYaw = Mathf.Atan2(horizontalToPlayer.x, horizontalToPlayer.z) * Mathf.Rad2Deg;
            }

            // Calculate roll based on the player's x position.
            // When the player's x equals PlayerMovement.sideMoveRange, roll becomes -maxRollAngle,
            // and when it equals -sideMoveRange, roll becomes maxRollAngle.
            float sideRange = PlayerMovement.Instance.sideMoveRange;
            if (Mathf.Abs(sideRange) < 0.001f)
                sideRange = 1f; // Prevent division by zero.
            float computedRoll = -(PlayerMovement.Instance.transform.position.x / sideRange) * maxRollAngle;

            // Build the target rotation: fixed pitch, dynamic yaw and roll.
            Quaternion targetRotation = Quaternion.Euler(cameraPitchOffset, desiredYaw, computedRoll);

            // Smoothly interpolate towards the target rotation.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
        }
    }
}
