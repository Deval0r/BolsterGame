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

    private Camera mainCamera;
    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private Vector3 targetPosition;
    private bool wasHoldingLastFrame;

    private void Start()
    {
        mainCamera = Camera.main;
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
            // Check if the object has a rigidbody
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && rb.mass <= maxHoldMass)
            {
                heldObject = hit.collider.gameObject;
                heldRigidbody = rb;
                
                // Disable gravity and make it kinematic while held
                heldRigidbody.useGravity = false;
                heldRigidbody.isKinematic = true;
            }
        }
    }

    private void UpdateHeldObjectPosition()
    {
        // Calculate the target position in front of the camera
        targetPosition = mainCamera.transform.position + mainCamera.transform.forward * holdDistance;

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
        }

        heldObject = null;
        heldRigidbody = null;
    }
} 