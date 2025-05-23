using UnityEngine;

public class ObjectPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 5f; // How far the raycast will check
    [SerializeField] private float holdDistance = 2f; // How far in front of camera to hold objects
    [SerializeField] private float smoothSpeed = 10f; // How smoothly the object moves to hold position
    [SerializeField] private LayerMask pickupableLayers; // Which layers can be picked up

    [Header("Physics Settings")]
    [SerializeField] private float throwForce = 10f; // How hard to throw when releasing
    [SerializeField] private float maxHoldMass = 10f; // Maximum mass of objects that can be picked up

    [Header("Movement Penalty Settings")]
    [SerializeField] private float maxSpeedPenalty = 0.25f; // Minimum speed multiplier (1/4 speed)
    [SerializeField] private float minSpeedPenalty = 0.75f; // Maximum speed multiplier (3/4 speed)
    [SerializeField] private float maxMassForMinPenalty = 2f; // Mass at which minimum penalty is applied
    [SerializeField] private float maxMassForMaxPenalty = 10f; // Mass at which maximum penalty is applied

    [Header("Collision Settings")]
    [SerializeField] private float minHoldDistance = 0.5f; // Minimum distance to hold objects
    [SerializeField] private float collisionCheckRadius = 0.3f; // Radius to check for collisions
    [SerializeField] private float maxPushForce = 5f; // Maximum force before dropping object
    [SerializeField] private LayerMask collisionLayers; // Layers to check for collisions

    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] pickupSounds;
    [SerializeField] private float minPickupPitch = 0.9f;
    [SerializeField] private float maxPickupPitch = 1.1f;
    [SerializeField] private float minTimeAfterCollision = 0.5f; // Minimum time after collision before pickup sound can play

    private Camera mainCamera;
    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private Vector3 targetPosition;
    private bool wasHoldingLastFrame;
    private Movement playerMovement; // Reference to Movement script
    private Vector3 lastSafePosition;
    private float currentHoldDistance;
    private AudioSource playerAudioSource;
    private float lastCollisionTime;
    private bool wasDropped;

    public bool IsHoldingObject => heldObject != null;
    public float HeldObjectMass => heldRigidbody != null ? heldRigidbody.mass : 0f;

    private void Start()
    {
        mainCamera = Camera.main;
        playerMovement = GetComponent<Movement>(); // Get reference to Movement script
        currentHoldDistance = holdDistance;
        lastSafePosition = transform.position;
        playerAudioSource = GetComponent<AudioSource>();
        lastCollisionTime = 0f; // Initialize to 0 to allow first pickup sound
    }

    private void Update()
    {
        bool isHolding = Input.GetKey(KeyCode.F);

        // If we're not holding anything and the key is pressed, try to pick up
        if (isHolding && heldObject == null)
        {
            TryPickupObject();
        }
        // If we were holding and released the key, drop the object
        else if (!isHolding && heldObject != null)
        {
            DropObject();
        }

        // Update held object position if we're holding something
        if (heldObject != null)
        {
            UpdateHeldObjectPosition();
        }

        wasHoldingLastFrame = isHolding;
    }

    private void TryPickupObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, pickupableLayers))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && rb.mass <= maxHoldMass)
            {
                // Check if the initial pickup position is valid
                Vector3 initialPosition = mainCamera.transform.position + mainCamera.transform.forward * holdDistance;
                if (Physics.CheckSphere(initialPosition, collisionCheckRadius, collisionLayers))
                {
                    // If initial position is blocked, try to find a closer valid position
                    float testDistance = holdDistance;
                    while (testDistance > minHoldDistance)
                    {
                        initialPosition = mainCamera.transform.position + mainCamera.transform.forward * testDistance;
                        if (!Physics.CheckSphere(initialPosition, collisionCheckRadius, collisionLayers))
                        {
                            break;
                        }
                        testDistance -= 0.1f;
                    }

                    // If we couldn't find a valid position, don't pick up
                    if (testDistance <= minHoldDistance)
                    {
                        return;
                    }
                }

                heldObject = hit.collider.gameObject;
                heldRigidbody = rb;
                currentHoldDistance = holdDistance;
                
                // Disable gravity and make it kinematic while held
                heldRigidbody.useGravity = false;
                heldRigidbody.isKinematic = true;

                // Apply movement penalty based on mass
                ApplyMovementPenalty(rb.mass);

                // Play pickup sound if conditions are met
                if (ShouldPlayPickupSound())
                {
                    PlayPickupSound();
                }
            }
        }
    }

    private bool ShouldPlayPickupSound()
    {
        // If the object hasn't collided recently (or ever), play the sound
        bool timeCheck = Time.time - lastCollisionTime > minTimeAfterCollision || lastCollisionTime == 0f;
        
        // Debug logging
        if (!timeCheck)
        {
            Debug.Log($"Pickup sound skipped: Time since last collision ({Time.time - lastCollisionTime:F2}s) < min time ({minTimeAfterCollision}s)");
        }
        if (wasDropped)
        {
            Debug.Log("Pickup sound skipped: Object was just dropped");
        }
        
        return timeCheck && !wasDropped;
    }

    private void PlayPickupSound()
    {
        if (playerAudioSource != null && pickupSounds != null && pickupSounds.Length > 0)
        {
            // Get random sound from array
            AudioClip randomSound = pickupSounds[Random.Range(0, pickupSounds.Length)];
            
            // Randomize pitch
            playerAudioSource.pitch = Random.Range(minPickupPitch, maxPickupPitch);
            
            // Play the sound
            playerAudioSource.PlayOneShot(randomSound);
            Debug.Log("Pickup sound played successfully");
        }
        else
        {
            Debug.LogWarning("Could not play pickup sound: AudioSource or sounds not set up properly");
        }
    }

    private void UpdateHeldObjectPosition()
    {
        if (heldObject == null) return;

        // Calculate the ideal target position
        Vector3 idealPosition = mainCamera.transform.position + mainCamera.transform.forward * currentHoldDistance;
        
        // Check for collisions between current position and ideal position
        Vector3 direction = idealPosition - heldObject.transform.position;
        float distance = direction.magnitude;
        
        if (distance > 0.01f) // Only check if we're actually moving
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                heldObject.transform.position,
                collisionCheckRadius,
                direction.normalized,
                distance,
                collisionLayers
            );

            // Check if any hit is not the held object itself
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject != heldObject && hit.collider.gameObject != gameObject)
                {
                    // If we hit something, reduce the hold distance
                    float hitDistance = hit.distance;
                    currentHoldDistance = Mathf.Min(currentHoldDistance, hitDistance);
                    
                    // If we're too close to the player, drop the object
                    if (hitDistance < minHoldDistance)
                    {
                        DropObject();
                        return;
                    }
                    
                    // If the object is pushing against something with significant force, drop it
                    if (heldRigidbody != null)
                    {
                        float pushForce = heldRigidbody.mass * Physics.gravity.magnitude;
                        if (pushForce > maxPushForce)
                        {
                            DropObject();
                            return;
                        }
                    }
                    
                    break;
                }
            }
        }

        // If no collisions, gradually return to ideal hold distance
        if (currentHoldDistance < holdDistance)
        {
            currentHoldDistance = Mathf.Lerp(currentHoldDistance, holdDistance, Time.deltaTime * smoothSpeed);
        }

        // Calculate final target position with current hold distance
        targetPosition = mainCamera.transform.position + mainCamera.transform.forward * currentHoldDistance;

        // Smoothly move the object to the target position
        heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        
        // Make the object face the same direction as the camera
        heldObject.transform.rotation = Quaternion.Lerp(heldObject.transform.rotation, mainCamera.transform.rotation, smoothSpeed * Time.deltaTime);
    }

    private void DropObject()
    {
        if (heldRigidbody != null)
        {
            // Re-enable physics
            heldRigidbody.isKinematic = false;
            heldRigidbody.useGravity = true;

            // Add force in the direction the camera is facing
            heldRigidbody.AddForce(mainCamera.transform.forward * throwForce, ForceMode.Impulse);
            
            // Mark that this object was just dropped
            wasDropped = true;
        }

        // Reset movement speed when dropping object
        playerMovement.ResetSpeedMultiplier();

        heldObject = null;
        heldRigidbody = null;
    }

    private void ApplyMovementPenalty(float mass)
    {
        // Calculate speed multiplier based on mass
        float normalizedMass = Mathf.Clamp01((mass - maxMassForMinPenalty) / (maxMassForMaxPenalty - maxMassForMinPenalty));
        float speedMultiplier = Mathf.Lerp(minSpeedPenalty, maxSpeedPenalty, normalizedMass);
        
        // Apply the speed multiplier to the player's movement
        playerMovement.SetSpeedMultiplier(speedMultiplier);
    }

    public bool CanLiftObject(GameObject obj)
    {
        if (obj == null) return false;
        
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) return false;
        
        // Check if the object's mass is within the liftable range
        if (rb.mass > maxHoldMass) return false;
        
        // Check if the object is on a liftable layer
        if (((1 << obj.layer) & pickupableLayers) == 0) return false;
        
        return true;
    }

    // Called by CollisionSound when the object collides
    public void OnObjectCollision()
    {
        lastCollisionTime = Time.time;
        wasDropped = false;
    }
} 