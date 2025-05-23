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
    [SerializeField] private float boardBreakDelay = 0.5f; // Time to hold before breaking a board
    [SerializeField] private KeyCode boardKey = KeyCode.Mouse0;
    [SerializeField] private float maxRandomRotation = 10f; // Maximum random rotation in degrees
    [SerializeField] private float heightMultiplier = 0.8f; // Multiplier to adjust board spacing relative to window height

    [Header("Audio Settings")]
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private AudioClip boardPlacementSound;
    [SerializeField] private AudioClip boardSwipeSound;
    [SerializeField] private AudioClip boardDestroySound;
    [SerializeField] private float breakSoundPitchVariation = 0.1f;
    [SerializeField] private float placeSoundPitchVariation = 0.05f;
    [SerializeField] private float swingSoundPitchVariation = 0.1f;

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
    private bool isBreakingBoard;
    private float boardBreakStartTime;

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
                isBreakingBoard = false;
                
                // First check for boards
                RaycastHit boardHit;
                if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out boardHit, maxBoardDistance, boardLayer))
                {
                    // Found a board, play swing sound and start break timer
                    currentTargetBoard = boardHit.collider.gameObject;
                    currentTargetWindow = null;
                    PlaySound(boardSwipeSound, swingSoundPitchVariation);
                    isBreakingBoard = true;
                    boardBreakStartTime = Time.time;
                    Debug.Log("Found board to break");
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
                            PlaySound(boardPlacementSound, placeSoundPitchVariation);
                            Debug.Log("Found window to board");
                        }
                        else
                        {
                            // Window is fully boarded, play swipe sound
                            PlaySound(boardSwipeSound, swingSoundPitchVariation);
                            Debug.Log("Window is fully boarded");
                        }
                    }
                    else
                    {
                        // Not looking at anything, play swipe sound
                        PlaySound(boardSwipeSound, swingSoundPitchVariation);
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
                lastBoardTime = Time.time;
                
                // Stop placement sound if it's playing and we didn't place a board
                if (isPlayingPlacementSound && !canPlaceBoard && playerAudioSource != null)
                {
                    playerAudioSource.Stop();
                    isPlayingPlacementSound = false;
                }

                // Reset breaking state
                isBreakingBoard = false;
            }
        }

        // Check if we're breaking a board
        if (isBreakingBoard && currentTargetBoard != null)
        {
            // Check if we're still looking at the same board
            RaycastHit boardHit;
            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out boardHit, maxBoardDistance, boardLayer))
            {
                if (boardHit.collider.gameObject == currentTargetBoard)
                {
                    // If we've held long enough, break the board
                    if (Time.time >= boardBreakStartTime + boardBreakDelay)
                    {
                        PlaySound(boardDestroySound, breakSoundPitchVariation);
                        Destroy(currentTargetBoard);
                        currentTargetBoard = null;
                        isBreakingBoard = false;
                        Debug.Log("Board broken");
                    }
                }
                else
                {
                    // Looking at a different board, reset break timer
                    isBreakingBoard = false;
                }
            }
            else
            {
                // Not looking at the board anymore, reset break timer
                isBreakingBoard = false;
            }
        }

        if (isHoldingBoardKey && Time.time >= lastBoardTime + boardPlacementCooldown)
        {
            // Check if we've held long enough for board placement
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

    private void PlaySound(AudioClip clip, float pitchVariation)
    {
        if (playerAudioSource != null && clip != null)
        {
            playerAudioSource.clip = clip;
            playerAudioSource.loop = false;
            // Apply random pitch variation
            playerAudioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
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

        // Get window height from its scale
        float windowHeight = window.transform.localScale.y * heightMultiplier;
        
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

    public bool CanBoardWindow(GameObject window)
    {
        if (window == null) return false;
        
        // Check if we're holding the hammer
        if (!IsHoldingHammer()) return false;
        
        // Check if the window is already fully boarded
        if (windowBoards.ContainsKey(window) && windowBoards[window].Count >= 3)
        {
            return false;
        }
        
        return true;
    }
} 