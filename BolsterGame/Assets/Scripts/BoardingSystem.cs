using UnityEngine;
using System.Collections.Generic;

public class BoardingSystem : MonoBehaviour
{
    [Header("Boarding Settings")]
    [SerializeField] private float maxBoardDistance = 3f;
    [SerializeField] private LayerMask windowLayer;
    [SerializeField] private GameObject boardPrefab;
    [SerializeField] private float boardPlacementCooldown = 0.5f;
    [SerializeField] private float holdTimeRequired = 0.5f; // Time required to hold before placing
    [SerializeField] private KeyCode boardKey = KeyCode.Mouse0;
    [SerializeField] private float maxRandomRotation = 10f; // Maximum random rotation in degrees
    [SerializeField] private float windowHeight = 2f; // Approximate height of the window

    private Camera mainCamera;
    private float lastBoardTime;
    private bool isHoldingBoardKey;
    private float holdStartTime;
    private HotbarSystem hotbarSystem;
    private Dictionary<GameObject, List<GameObject>> windowBoards = new Dictionary<GameObject, List<GameObject>>();

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
        GameObject window = hit.collider.gameObject;
        
        // Initialize the list for this window if it doesn't exist
        if (!windowBoards.ContainsKey(window))
        {
            windowBoards[window] = new List<GameObject>();
        }

        // Check if we've reached the maximum number of boards for this window
        if (windowBoards[window].Count >= 3)
        {
            Debug.Log("Maximum number of boards reached for this window!");
            return;
        }

        // Calculate the position and rotation for the board
        Vector3 windowForward = hit.normal;
        Vector3 windowRight = Vector3.Cross(windowForward, Vector3.up).normalized;
        Vector3 windowUp = Vector3.Cross(windowRight, windowForward).normalized;

        // Calculate evenly spaced positions
        float totalHeight = windowHeight;
        float sectionHeight = totalHeight / 4f; // Divide into 4 sections (3 boards with gaps)
        
        // Calculate vertical position based on board count
        float verticalOffset;
        switch (windowBoards[window].Count)
        {
            case 0: // First board
                verticalOffset = -sectionHeight; // Bottom section
                break;
            case 1: // Second board
                verticalOffset = 0f; // Middle section
                break;
            case 2: // Third board
                verticalOffset = sectionHeight; // Top section
                break;
            default:
                return;
        }

        Vector3 boardPosition = hit.point + windowUp * verticalOffset;

        // Calculate base rotation
        Quaternion baseRotation = Quaternion.LookRotation(windowForward);
        
        // Add random rotation around the forward axis
        float randomRotation = Random.Range(-maxRandomRotation, maxRandomRotation);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(0, 0, randomRotation);

        // Instantiate the board
        GameObject board = Instantiate(boardPrefab, boardPosition, finalRotation);

        // Parent the board to the window
        board.transform.parent = window.transform;

        // Add to the list of boards for this window
        windowBoards[window].Add(board);

        Debug.Log($"Placed board {windowBoards[window].Count}/3 on window");
    }
} 