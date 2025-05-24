using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class MonsterAI : MonoBehaviour
{
    [Header("Core Settings")]
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float decisionUpdateInterval = 1f;
    [SerializeField] private float deceptionChance = 0.3f;
    [SerializeField] private float patienceTime = 5f;

    [Header("Entry Point Settings")]
    [SerializeField] private LayerMask windowLayer; // Layer for windows
    [SerializeField] private LayerMask boardLayer; // Layer for boards
    [SerializeField] private LayerMask obstacleLayer; // Layer for obstacles (furniture, etc)
    [SerializeField] private float entryPointCheckRadius = 1f;
    [SerializeField] private float verticalReach = 3f;

    [Header("NavMesh Settings")]
    [SerializeField] private float walkableAreaCost = 1f;
    [SerializeField] private float obstacleAreaCost = 5f;
    [SerializeField] private float windowAreaCost = 3f;
    [SerializeField] private float jumpAreaCost = 2f; // Cost for jump/climb areas
    [SerializeField] private float jumpHeight = 2f; // Maximum height the monster can jump
    [SerializeField] private float jumpDistance = 3f; // Maximum distance the monster can jump

    [Header("Deception Settings")]
    [SerializeField] private AudioClip[] tapSounds;
    [SerializeField] private AudioClip[] creakSounds;
    [SerializeField] private float minDeceptionDelay = 2f;
    [SerializeField] private float maxDeceptionDelay = 5f;

    private NavMeshAgent agent;
    private AudioSource audioSource;
    private Transform player;
    private Dictionary<GameObject, float> entryPointStrengths = new Dictionary<GameObject, float>();
    private GameObject currentTarget;
    private float lastDecisionTime;
    private bool isDeceiving;
    private float deceptionTimer;
    private Vector3 lastKnownPlayerPosition;
    private MonsterState currentState = MonsterState.Patrolling;
    private bool isJumping;
    private Vector3 jumpStartPosition;
    private Vector3 jumpTargetPosition;
    private float jumpStartTime;
    private float jumpDuration = 0.5f; // Duration of jump animation

    private enum MonsterState
    {
        Patrolling,
        Assessing,
        Deceiving,
        Attacking,
        Searching,
        Jumping
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
        }

        // Configure NavMeshAgent
        if (agent != null)
        {
            agent.areaMask = NavMesh.AllAreas; // Use all areas
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.radius = 0.5f; // Adjust based on monster size
            agent.height = 2f; // Adjust based on monster size
            agent.baseOffset = 0f; // Adjust if monster needs to float
            agent.autoTraverseOffMeshLink = false; // We'll handle jumping manually
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Update last known player position if in range
        if (Vector3.Distance(transform.position, player.position) <= detectionRange)
        {
            lastKnownPlayerPosition = player.position;
        }

        // State machine
        switch (currentState)
        {
            case MonsterState.Patrolling:
                HandlePatrolling();
                break;
            case MonsterState.Assessing:
                HandleAssessing();
                break;
            case MonsterState.Deceiving:
                HandleDeceiving();
                break;
            case MonsterState.Attacking:
                HandleAttacking();
                break;
            case MonsterState.Searching:
                HandleSearching();
                break;
            case MonsterState.Jumping:
                HandleJumping();
                break;
        }

        // Update decisions periodically
        if (Time.time - lastDecisionTime >= decisionUpdateInterval)
        {
            UpdateDecision();
            lastDecisionTime = Time.time;
        }
    }

    private void HandlePatrolling()
    {
        // Basic patrol behavior
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            Vector3 randomPoint = Random.insideUnitSphere * 10f;
            randomPoint += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void HandleAssessing()
    {
        // Scan for windows and evaluate their strength
        Collider[] windows = Physics.OverlapSphere(transform.position, detectionRange, windowLayer);
        foreach (Collider window in windows)
        {
            float strength = EvaluateEntryPointStrength(window.gameObject);
            if (entryPointStrengths.ContainsKey(window.gameObject))
            {
                entryPointStrengths[window.gameObject] = strength;
            }
            else
            {
                entryPointStrengths.Add(window.gameObject, strength);
            }
        }

        // Find the weakest entry point
        GameObject weakestEntry = entryPointStrengths
            .OrderBy(x => x.Value)
            .FirstOrDefault().Key;

        if (weakestEntry != null)
        {
            currentTarget = weakestEntry;
        }

        // Decide whether to deceive or attack
        if (Random.value < deceptionChance)
        {
            currentState = MonsterState.Deceiving;
            StartDeception();
        }
        else
        {
            currentState = MonsterState.Attacking;
        }
    }

    private void HandleDeceiving()
    {
        if (!isDeceiving)
        {
            StartDeception();
        }

        deceptionTimer -= Time.deltaTime;
        if (deceptionTimer <= 0)
        {
            isDeceiving = false;
            currentState = MonsterState.Assessing;
        }
    }

    private void HandleAttacking()
    {
        if (currentTarget != null)
        {
            // Check if we need to jump to reach the target
            if (CanJumpTo(currentTarget.transform.position))
            {
                jumpTargetPosition = currentTarget.transform.position;
                currentState = MonsterState.Jumping;
                return;
            }

            // Move to target and attempt to break in
            agent.SetDestination(currentTarget.transform.position);
            
            if (Vector3.Distance(transform.position, currentTarget.transform.position) < 2f)
            {
                AttemptBreakIn();
            }
        }
        else
        {
            currentState = MonsterState.Assessing;
        }
    }

    private void HandleSearching()
    {
        // Search for player at last known position
        agent.SetDestination(lastKnownPlayerPosition);
        
        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 1f)
        {
            currentState = MonsterState.Patrolling;
        }
    }

    private void HandleJumping()
    {
        if (!isJumping)
        {
            // Start jump
            isJumping = true;
            jumpStartPosition = transform.position;
            jumpStartTime = Time.time;
            agent.enabled = false; // Disable NavMeshAgent during jump
        }

        // Calculate jump progress
        float jumpProgress = (Time.time - jumpStartTime) / jumpDuration;
        
        if (jumpProgress <= 1f)
        {
            // Calculate jump arc
            float height = jumpHeight * Mathf.Sin(jumpProgress * Mathf.PI);
            Vector3 newPosition = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, jumpProgress);
            newPosition.y += height;
            transform.position = newPosition;
        }
        else
        {
            // Jump complete
            isJumping = false;
            agent.enabled = true;
            currentState = MonsterState.Assessing;
        }
    }

    private bool CanJumpTo(Vector3 targetPosition)
    {
        // Check if target is within jump range
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance > jumpDistance) return false;

        // Check if there's a clear path
        Vector3 direction = targetPosition - transform.position;
        float height = targetPosition.y - transform.position.y;
        if (height > jumpHeight) return false;

        // Check for obstacles in the way
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction.normalized, distance, obstacleLayer);
        return hits.Length == 0;
    }

    private void UpdateDecision()
    {
        // Check if player is in range
        if (Vector3.Distance(transform.position, player.position) <= detectionRange)
        {
            currentState = MonsterState.Assessing;
        }
        else
        {
            currentState = MonsterState.Patrolling;
        }
    }

    private float EvaluateEntryPointStrength(GameObject entryPoint)
    {
        float strength = 0f;
        
        // Check for boards
        BoardingSystem boardingSystem = FindObjectOfType<BoardingSystem>();
        if (boardingSystem != null)
        {
            strength += boardingSystem.GetWindowBoardCount(entryPoint) * 0.5f;
        }

        // Check for obstacles near the window
        Collider[] obstacles = Physics.OverlapSphere(entryPoint.transform.position, entryPointCheckRadius, obstacleLayer);
        strength += obstacles.Length * 0.3f;

        return strength;
    }

    private void StartDeception()
    {
        isDeceiving = true;
        deceptionTimer = Random.Range(minDeceptionDelay, maxDeceptionDelay);

        // Choose a random deception sound
        if (audioSource != null)
        {
            AudioClip[] sounds = Random.value < 0.5f ? tapSounds : creakSounds;
            if (sounds != null && sounds.Length > 0)
            {
                audioSource.PlayOneShot(sounds[Random.Range(0, sounds.Length)]);
            }
        }
    }

    private void AttemptBreakIn()
    {
        // Implement break-in logic here
        // This will need to interact with the BoardingSystem
        Debug.Log("Attempting to break in at: " + currentTarget.name);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
} 