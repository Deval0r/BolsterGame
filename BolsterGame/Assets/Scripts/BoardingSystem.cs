using UnityEngine;

public class BoardingSystem : MonoBehaviour
{
    [Header("Boarding Settings")]
    [SerializeField] private float maxBoardDistance = 3f;
    [SerializeField] private LayerMask windowLayer;
    [SerializeField] private GameObject boardPrefab;
    [SerializeField] private float boardPlacementCooldown = 0.5f;
    [SerializeField] private float holdTimeRequired = 0.5f; // Time required to hold before placing
    [SerializeField] private KeyCode boardKey = KeyCode.Mouse0;

    private Camera mainCamera;
    private float lastBoardTime;
    private bool isHoldingBoardKey;
    private float holdStartTime;
    private HotbarSystem hotbarSystem;

    private void Start()
    {
        mainCamera = Camera.main;
        hotbarSystem = GetComponent<HotbarSystem>();
        if (hotbarSystem == null)
        {
            Debug.LogError("HotbarSystem not found on the same GameObject as BoardingSystem!");
        }
    }

    private void Update()
    {
        // Check if player is holding the hammer
        bool isHoldingHammer = IsHoldingHammer();
        
        // Check for board placement input
        if (Input.GetKeyDown(boardKey))
        {
            isHoldingBoardKey = true;
            holdStartTime = Time.time;
        }
        else if (Input.GetKeyUp(boardKey))
        {
            isHoldingBoardKey = false;
            lastBoardTime = Time.time; // Reset the cooldown when releasing
        }

        if (isHoldingBoardKey && Time.time >= lastBoardTime + boardPlacementCooldown)
        {
            // Check if we've held long enough
            if (Time.time >= holdStartTime + holdTimeRequired)
            {
                TryPlaceBoard();
                lastBoardTime = Time.time;
            }
        }
    }

    private bool IsHoldingHammer()
    {
        if (hotbarSystem == null)
        {
            Debug.LogWarning("HotbarSystem is null!");
            return false;
        }
        
        bool isHammer = hotbarSystem.IsHoldingHammer();
        if (isHammer)
        {
            Debug.Log("Hammer is selected!");
        }
        return isHammer;
    }

    private void TryPlaceBoard()
    {
        RaycastHit hit;
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, maxBoardDistance, windowLayer))
        {
            // Check if we hit a window
            if (hit.collider.CompareTag("Window"))
            {
                Debug.Log("Hit window, placing board!");
                PlaceBoard(hit);
            }
            else
            {
                Debug.Log("Hit something but not a window. Tag: " + hit.collider.tag);
            }
        }
        else
        {
            Debug.Log("No window in range");
        }
    }

    private void PlaceBoard(RaycastHit hit)
    {
        // Calculate the position and rotation for the board
        Vector3 boardPosition = hit.point;
        
        // Get the window's forward direction (normal of the hit surface)
        Vector3 windowForward = hit.normal;
        
        // Calculate the rotation to align with the window
        Quaternion boardRotation = Quaternion.LookRotation(windowForward);
        
        // Instantiate the board
        GameObject board = Instantiate(boardPrefab, boardPosition, boardRotation);

        // Parent the board to the window
        board.transform.parent = hit.collider.transform;

        // Adjust the board's position to be centered on the window
        board.transform.localPosition = Vector3.zero;
        
        // No need for additional rotation since the board prefab should be oriented correctly
    }
} 