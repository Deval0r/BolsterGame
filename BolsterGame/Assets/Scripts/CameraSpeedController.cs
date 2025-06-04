using UnityEngine;

public class CameraSpeedController : MonoBehaviour
{
    public static CameraSpeedController Instance { get; private set; }

    [Header("Camera Speed Settings")]
    public float maxSpeed = 10f;
    public float acceleration = 5f; // How quickly the camera accelerates

    [Header("Camera X Spring Settings")]
    public float xSpringStrength = 20f;
    public float xDamping = 4f;

    public float CurrentSpeed { get; private set; } = 0f;

    private float targetX = 0f;
    private float velocityX = 0f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        // Directly follow player's x position
        if (PlayerMovement.Instance != null)
        {
            Vector3 pos = transform.position;
            pos.x = PlayerMovement.Instance.transform.position.x;
            transform.position = pos;
        }

        // Z movement (endless runner)
        CurrentSpeed += acceleration * (1f - (CurrentSpeed / maxSpeed)) * Time.deltaTime;
        CurrentSpeed = Mathf.Clamp(CurrentSpeed, 0f, maxSpeed);
        transform.position += Vector3.forward * CurrentSpeed * Time.deltaTime;
    }
} 