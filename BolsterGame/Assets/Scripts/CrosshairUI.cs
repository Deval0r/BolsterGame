using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Textures")]
    [SerializeField] private Texture2D defaultCrosshair; // Dot for looking at nothing
    [SerializeField] private Texture2D breakCrosshair;   // Icon for looking at breakable objects
    [SerializeField] private Texture2D boardCrosshair;   // Icon for looking at barricadable windows
    [SerializeField] private Texture2D liftCrosshair;    // Icon for looking at liftable objects
    [SerializeField] private Texture2D holdingCrosshair; // Icon for when holding an object

    [Header("Layer Settings")]
    [SerializeField] private LayerMask breakableLayer;
    [SerializeField] private LayerMask windowLayer;
    [SerializeField] private LayerMask liftableLayer;
    [SerializeField] private float raycastDistance = 3f;

    private RawImage crosshairImage;
    private Camera mainCamera;
    private BoardingSystem boardingSystem;
    private ObjectPickup objectPickup;

    private void Start()
    {
        crosshairImage = GetComponent<RawImage>();
        if (crosshairImage == null)
        {
            Debug.LogError("CrosshairUI requires a RawImage component!");
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
            enabled = false;
            return;
        }

        // Get references to other systems
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            boardingSystem = player.GetComponent<BoardingSystem>();
            objectPickup = player.GetComponent<ObjectPickup>();
        }
    }

    private void Update()
    {
        UpdateCrosshair();
    }

    private void UpdateCrosshair()
    {
        // First check if we're holding something
        if (objectPickup != null && objectPickup.IsHoldingObject)
        {
            crosshairImage.texture = holdingCrosshair;
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, raycastDistance))
        {
            // Check for breakable objects first
            if (((1 << hit.collider.gameObject.layer) & breakableLayer) != 0)
            {
                crosshairImage.texture = breakCrosshair;
                return;
            }

            // Check for windows
            if (((1 << hit.collider.gameObject.layer) & windowLayer) != 0)
            {
                // Only show board crosshair if we can actually board it
                if (boardingSystem != null && boardingSystem.CanBoardWindow(hit.collider.gameObject))
                {
                    crosshairImage.texture = boardCrosshair;
                    return;
                }
            }

            // Check for liftable objects
            if (((1 << hit.collider.gameObject.layer) & liftableLayer) != 0)
            {
                // Only show lift crosshair if we can actually lift it
                if (objectPickup != null && objectPickup.CanLiftObject(hit.collider.gameObject))
                {
                    crosshairImage.texture = liftCrosshair;
                    return;
                }
            }
        }

        // Default crosshair if nothing special is being looked at
        crosshairImage.texture = defaultCrosshair;
    }
} 