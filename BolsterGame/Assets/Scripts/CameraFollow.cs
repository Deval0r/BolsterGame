using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float springStrength = 35f;
    [SerializeField] private float damping = 0.7f;

    [Header("Mass-based Smoothing")]
    [SerializeField] private float maxMassForMinSmooth = 2f; // Mass at which minimum smooth time is applied
    [SerializeField] private float maxMassForMaxSmooth = 10f; // Mass at which maximum smooth time is applied
    [SerializeField] private float minSmoothTime = 0.1f; // Minimum smooth time
    [SerializeField] private float maxSmoothTime = 0.3f; // Maximum smooth time
    [SerializeField] private float baseSmoothTime = 0.1f; // Base smooth time when not holding anything
    [SerializeField] private float minRotationSpeed = 200f; // Minimum rotation speed when holding heavy objects
    [SerializeField] private float maxRotationSpeed = 1000f; // Maximum rotation speed when not holding objects

    private Vector3 currentRotation;
    private Vector3 rotationVelocity;
    private Vector3 targetRotation;
    private Vector3 currentPosition;
    private Vector3 positionVelocity;
    private ObjectPickup objectPickup; // Reference to ObjectPickup script

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("No target assigned to CameraFollow script!");
        }
        currentRotation = transform.eulerAngles;
        targetRotation = currentRotation;
        currentPosition = transform.position;
        objectPickup = target.GetComponent<ObjectPickup>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Get mouse input from Movement script
        float mouseX = Input.GetAxis("Mouse X") * target.GetComponent<Movement>().MouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * target.GetComponent<Movement>().MouseSensitivity;

        // Calculate rotation speed multiplier based on held object mass
        float rotationSpeedMultiplier = CalculateRotationSpeedMultiplier();

        // Update target rotation based on mouse input with speed multiplier
        targetRotation.y += mouseX * rotationSpeedMultiplier;
        targetRotation.x -= mouseY * rotationSpeedMultiplier;
        targetRotation.x = Mathf.Clamp(targetRotation.x, -90f, 90f);

        // Calculate spring force for rotation
        Vector3 displacement = targetRotation - currentRotation;
        Vector3 springForce = displacement * springStrength;
        
        // Apply damping to rotation
        rotationVelocity = rotationVelocity * (1 - damping) + springForce * Time.deltaTime;
        
        // Clamp rotation velocity with mass-based speed limit
        float currentMaxRotationSpeed = Mathf.Lerp(minRotationSpeed, maxRotationSpeed, rotationSpeedMultiplier);
        rotationVelocity = Vector3.ClampMagnitude(rotationVelocity, currentMaxRotationSpeed);
        
        // Update current rotation
        currentRotation += rotationVelocity * Time.deltaTime;

        // Calculate desired camera position
        Vector3 desiredPosition = target.position;

        // Calculate current smooth time based on held object mass
        float currentSmoothTime = CalculateSmoothTime();

        // Smoothly move camera to desired position
        currentPosition = Vector3.SmoothDamp(currentPosition, desiredPosition, ref positionVelocity, currentSmoothTime);
        transform.position = currentPosition;

        // Apply rotation
        transform.rotation = Quaternion.Euler(currentRotation);
    }

    private float CalculateSmoothTime()
    {
        if (objectPickup != null && objectPickup.IsHoldingObject)
        {
            float mass = objectPickup.HeldObjectMass;
            // Calculate smooth time based on mass
            float normalizedMass = Mathf.Clamp01((mass - maxMassForMinSmooth) / (maxMassForMaxSmooth - maxMassForMinSmooth));
            return Mathf.Lerp(minSmoothTime, maxSmoothTime, normalizedMass);
        }
        return baseSmoothTime;
    }

    private float CalculateRotationSpeedMultiplier()
    {
        if (objectPickup != null && objectPickup.IsHoldingObject)
        {
            float mass = objectPickup.HeldObjectMass;
            // Calculate speed multiplier based on mass (inverse of smooth time)
            float normalizedMass = Mathf.Clamp01((mass - maxMassForMinSmooth) / (maxMassForMaxSmooth - maxMassForMinSmooth));
            return Mathf.Lerp(1f, 0.2f, normalizedMass); // 1.0 for light objects, 0.2 for heavy objects
        }
        return 1f;
    }
}