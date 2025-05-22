using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float springStrength = 35f; // Increased for snappier response
    [SerializeField] private float damping = 0.7f; // Increased for less overshoot
    [SerializeField] private float maxRotationSpeed = 1000f; // Limit maximum rotation speed

    private Vector3 currentRotation;
    private Vector3 rotationVelocity;
    private Vector3 targetRotation;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("No target assigned to CameraFollow script!");
        }
        currentRotation = transform.eulerAngles;
        targetRotation = currentRotation;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Always match position exactly
        transform.position = target.position;

        // Get mouse input from Movement script
        float mouseX = Input.GetAxis("Mouse X") * target.GetComponent<Movement>().MouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * target.GetComponent<Movement>().MouseSensitivity;

        // Update target rotation based on mouse input
        targetRotation.y += mouseX;
        targetRotation.x -= mouseY;
        targetRotation.x = Mathf.Clamp(targetRotation.x, -90f, 90f);

        // Calculate spring force
        Vector3 displacement = targetRotation - currentRotation;
        Vector3 springForce = displacement * springStrength;
        
        // Apply damping
        rotationVelocity = rotationVelocity * (1 - damping) + springForce * Time.deltaTime;
        
        // Clamp rotation velocity to prevent excessive spinning
        rotationVelocity = Vector3.ClampMagnitude(rotationVelocity, maxRotationSpeed);
        
        // Update current rotation
        currentRotation += rotationVelocity * Time.deltaTime;

        // Apply rotation
        transform.rotation = Quaternion.Euler(currentRotation);
    }
} 