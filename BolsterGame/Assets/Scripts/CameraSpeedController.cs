using UnityEngine;
using UnityEngine.UI;

public class CameraSpeedController : MonoBehaviour
{
    public static CameraSpeedController Instance { get; private set; }

    [Header("Camera Speed Settings")]
    public float maxSpeed = 10f;
    public float acceleration = 5f; // How quickly the forward/global speed increases
    public float boostSpeedMultiplier = 1.5f; // How much faster you go when boosting
    public float staminaDrainRate = 10f; // How fast stamina drains per second
    public float staminaRegenRate = 5f; // How fast stamina regenerates per second
    public float maxStamina = 100f; // Maximum stamina value

    [Header("Camera Follow Settings")]
    public float followSpeed = 5f;  // Smoothing factor for following the player's position
    public float zOffset = 10f;     // Offset on the z-axis relative to the player

    [Header("Camera Rotation Settings")]
    public float rotationSmoothSpeed = 5f; // Smoothing factor for rotation changes
    public float cameraPitchOffset = -45f; // Fixed pitch (x-axis rotation) offset
    public float maxRollAngle = 45f;        // Maximum roll angle (z-axis rotation) when player is at lane extremes

    [Header("FOV Settings")]
    public float normalFOV = 60f;
    public float boostFOV = 75f;
    public float fovChangeSpeed = 5f;

    [Header("UI References")]
    public Slider staminaBar;

    [Header("Audio Settings")]
    public AudioSource backgroundMusic;
    public float normalMusicPitch = 1f;
    public float boostMusicPitch = 1.2f;
    public float musicPitchChangeSpeed = 2f;

    public float CurrentSpeed { get; private set; } = 0f;
    private float currentStamina;
    private Camera mainCamera;
    private float targetFOV;
    private float targetMusicPitch;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
            mainCamera = Camera.main;

        currentStamina = maxStamina;
        targetFOV = normalFOV;
        targetMusicPitch = normalMusicPitch;
        
        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currentStamina;
        }

        // Find background music if not assigned
        if (backgroundMusic == null)
        {
            backgroundMusic = FindObjectOfType<AudioSource>();
        }
    }

    void Update()
    {
        HandleSpeedBoost();
        UpdateSpeed();
        UpdateFOV();
        UpdateStaminaUI();
        UpdateMusicPitch();
    }

    private void HandleSpeedBoost()
    {
        bool isBoosting = Input.GetKey(KeyCode.W) && currentStamina > 0;
        
        if (isBoosting)
        {
            currentStamina = Mathf.Max(0, currentStamina - staminaDrainRate * Time.deltaTime);
            targetFOV = boostFOV;
            targetMusicPitch = boostMusicPitch;
        }
        else
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            targetFOV = normalFOV;
            targetMusicPitch = normalMusicPitch;
        }
    }

    private void UpdateSpeed()
    {
        float targetSpeed = maxSpeed;
        if (Input.GetKey(KeyCode.W) && currentStamina > 0)
        {
            targetSpeed *= boostSpeedMultiplier;
        }

        CurrentSpeed += acceleration * (1f - (CurrentSpeed / targetSpeed)) * Time.deltaTime;
        CurrentSpeed = Mathf.Clamp(CurrentSpeed, 0f, targetSpeed);

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
                // Ensure the yaw is always positive to prevent the camera from rotating the wrong way
                if (desiredYaw < 0)
                {
                    desiredYaw += 360f;
                }
            }

            // Calculate roll based on the player's x position.
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

    private void UpdateFOV()
    {
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina;
        }
    }

    private void UpdateMusicPitch()
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.pitch = Mathf.Lerp(backgroundMusic.pitch, targetMusicPitch, Time.deltaTime * musicPitchChangeSpeed);
        }
    }
}
