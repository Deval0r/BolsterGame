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

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Check if player is holding the hammer (you'll need to implement this check)
        if (!IsHoldingHammer()) return;

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
        // TODO: Implement check for hammer item in hotbar
        return true; // Temporary return
    }

    private void TryPlaceBoard()
    {
        RaycastHit hit;
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, maxBoardDistance, windowLayer))
        {
            // Check if we hit a window
            if (hit.collider.CompareTag("Window"))
            {
                PlaceBoard(hit);
            }
        }
    }

    private void PlaceBoard(RaycastHit hit)
    {
        // Calculate the position and rotation for the board
        Vector3 boardPosition = hit.point;
        Quaternion boardRotation = Quaternion.LookRotation(hit.normal);

        // Instantiate the board
        GameObject board = Instantiate(boardPrefab, boardPosition, boardRotation);

        // Parent the board to the window
        board.transform.parent = hit.collider.transform;

        // Adjust the board's position to be centered on the window
        board.transform.localPosition = Vector3.zero;

        // Rotate the board 90 degrees around its forward axis
        board.transform.Rotate(0, 0, 90, Space.Self);
    }
} 