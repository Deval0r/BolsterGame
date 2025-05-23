using UnityEngine;
using System.Collections.Generic;

public class BoardingSystem : MonoBehaviour
{
    [Header("Boarding Settings")]
    [SerializeField] private float maxBoardDistance = 3f;
    [SerializeField] private LayerMask windowLayer;
    [SerializeField] private LayerMask boardLayer; // Add layer mask for boards
    [SerializeField] private GameObject boardPrefab;
    [SerializeField] private float boardPlacementCooldown = 0.5f;
    [SerializeField] private float holdTimeRequired = 0.5f; // Time required to hold before placing
    [SerializeField] private KeyCode boardKey = KeyCode.Mouse0;
    [SerializeField] private float maxRandomRotation = 10f; // Maximum random rotation in degrees
    [SerializeField] private float windowHeight = 2f; // Approximate height of the window

    [Header("Audio Settings")]
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private AudioClip boardPlacementSound;
    [SerializeField] private AudioClip boardSwipeSound;
    [SerializeField] private AudioClip boardDestroySound;

    private Camera mainCamera;
    private float lastBoardTime;
    private bool isHoldingBoardKey;
    private float holdStartTime;
    private HotbarSystem hotbarSystem;
    private Dictionary<GameObject, List<GameObject>> windowBoards = new Dictionary<GameObject, List<GameObject>>();
    private bool isPlayingPlacementSound;
    private bool canPlaceBoard;
    private GameObject currentTargetWindow;
    private GameObject currentTargetBoard;

    private void Start()
    {
        mainCamera = Camera.main;
        hotbarSystem = GetComponent<HotbarSystem>();
        if (hotbarSystem == null)
        {
            Debug.LogError("HotbarSystem not found on the same GameObject as BoardingSystem!");
        }

        // Get AudioSource if not assigned
        if (playerAudioSource == null)
        {
            playerAudioSource = GetComponent<AudioSource>();
            if (playerAudioSource == null)
            {
                Debug.LogWarning("No AudioSource found on player! Board placement sounds will not play.");
            }
        }
    }

    private void Update()
    {
        // Check if player is holding the hammer
        bool isHoldingHammer = IsHoldingHammer();
        
        // Check for board placement input
        if (Input.GetKeyDown(boardKey))
        {
            if (isHoldingHammer)
            {
                isHoldingBoardKey = true;
                holdStartTime = Time.time;
                canPlaceBoard = false;
                
                // First check for boards
                RaycastHit boardHit;
                if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out boardHit, maxBoardDistance, boardLayer))
                {
                    // Found a board, play destroy sound
                    currentTargetBoard = boardHit.collider.gameObject;
                    currentTargetWindow = null;
                    PlaySound(boardDestroySound);
                    Debug.Log("Found board to destroy");
                }
                else
                {
                    // If no board found, check for windows
                    RaycastHit windowHit;
                    if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out windowHit, maxBoardDistance, windowLayer))
                    {
                        GameObject window = windowHit.collider.gameObject;
                        if (!windowBoards.ContainsKey(window) || windowBoards[window].Count < 3)
                        {
                            // Window is not fully boarded, play placement sound
                            currentTargetWindow = window;
                            currentTargetBoard = null;
                            PlaySound(boardPlacementSound);
                            Debug.Log("Found window to board");
                        }
                        else
                        {
                            // Window is fully boarded, play swipe sound
                            PlaySound(boardSwipeSound);
                            Debug.Log("Window is fully boarded");
                        }
                    }
                    else
                    {
                        // Not looking at anything, play swipe sound
                        PlaySound(boardSwipeSound);
                        Debug.Log("Not looking at anything");
                    }
                }
            }
        }
        else if (Input.GetKeyUp(boardKey))
        {
            if (isHoldingBoardKey)
            {
                isHoldingBoardKey = false;
                lastBoardTime = Time.time; // Reset the cooldown when releasing
                
                // Stop placement sound if it's playing and we didn't place a board
                if (isPlayingPlacementSound && !canPlaceBoard && playerAudioSource != null)
                {
                    playerAudioSource.Stop();
                    isPlayingPlacementSound = false;
                }

                // If we were targeting a board, destroy it
                if (currentTargetBoard != null)
                {
                    Destroy(currentTargetBoard);
                    currentTargetBoard = null;
                    Debug.Log("Destroyed board");
                }
            }
        }

        if (isHoldingBoardKey && Time.time >= lastBoardTime + boardPlacementCooldown)
        {
            // Check if we've held long enough
            if (Time.time >= holdStartTime + holdTimeRequired)
            {
                canPlaceBoard = true;
                TryPlaceBoard();
                lastBoardTime = Time.time;
            }
        }

        // Check for destroyed boards and update counts
        CheckForDestroyedBoards();
    }

    private void PlaySound(AudioClip clip)
    {
        if (playerAudioSource != null && clip != null)
        {
            playerAudioSource.clip = clip;
            playerAudioSource.loop = false;
            playerAudioSource.Play();
            isPlayingPlacementSound = (clip == boardPlacementSound);
        }
    }

    private void CheckForDestroyedBoards()
    {
        // Create a list of windows to check
        List<GameObject> windowsToCheck = new List<GameObject>(windowBoards.Keys);

        foreach (GameObject window in windowsToCheck)
        {
            if (window == null) continue;

            // Remove any null entries (destroyed boards)
            windowBoards[window].RemoveAll(board => board == null);

            // If no boards left, remove the window entry
            if (windowBoards[window].Count == 0)
            {
                windowBoards.Remove(window);
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
        if (currentTargetWindow != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, maxBoardDistance, windowLayer))
            {
                // Check if we hit the same window
                if (hit.collider.gameObject == currentTargetWindow)
                {
                    Debug.Log("Hit window, placing board!");
                    PlaceBoard(hit);
                }
            }
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