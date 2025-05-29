// Updated MonsterAI Script with improved jumping logic, entry switching, and board breaking

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class MonsterAI : MonoBehaviour
{
    [Header("Core Settings")]
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float decisionUpdateInterval = 1f;
    [SerializeField] private float deceptionChance = 0.3f;

    [Header("Entry Point Settings")]
    [SerializeField] private LayerMask windowLayer;
    [SerializeField] private LayerMask boardLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float entryPointCheckRadius = 1f;
    [SerializeField] private float verticalReach = 3f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float jumpDistance = 3f;
    [SerializeField] private float jumpCooldown = 2f;

    [Header("Deception Settings")]
    [SerializeField] private AudioClip[] tapSounds;
    [SerializeField] private AudioClip[] creakSounds;
    [SerializeField] private float minDeceptionDelay = 2f;
    [SerializeField] private float maxDeceptionDelay = 5f;

    private Rigidbody rb;
    private AudioSource audioSource;
    private Transform player;
    private Dictionary<GameObject, float> entryPointStrengths = new();
    private GameObject currentTarget;
    private float lastDecisionTime;
    private bool isDeceiving;
    private float deceptionTimer;
    private MonsterState currentState = MonsterState.Patrolling;
    private bool isJumping;
    private float lastJumpTime;
    private Vector3 jumpStartPosition;
    private Vector3 jumpTargetPosition;
    private float jumpStartTime;
    private float jumpDuration = 0.5f;
    private int attackAttempts;
    private float attackCooldown = 3f;
    private float lastAttackTime;

    private enum MonsterState { Patrolling, Assessing, Deceiving, Attacking, Searching, Jumping }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (!player) return;

        switch (currentState)
        {
            case MonsterState.Patrolling: HandlePatrolling(); break;
            case MonsterState.Assessing: HandleAssessing(); break;
            case MonsterState.Deceiving: HandleDeceiving(); break;
            case MonsterState.Attacking: HandleAttacking(); break;
            case MonsterState.Searching: HandleSearching(); break;
            case MonsterState.Jumping: HandleJumping(); break;
        }

        if (Time.time - lastDecisionTime >= decisionUpdateInterval)
        {
            UpdateDecision();
            lastDecisionTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        if (currentState == MonsterState.Jumping || currentTarget == null) return;

        Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
        direction.y = 0;
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, distanceToTarget, obstacleLayer))
        {
            rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            currentState = MonsterState.Assessing;
            currentTarget = null;
        }
    }

    private void HandlePatrolling()
    {
        if (currentTarget == null || Vector3.Distance(transform.position, currentTarget.transform.position) < 1f)
        {
            Vector3 patrolPoint;
            for (int i = 0; i < 10; i++)
            {
                patrolPoint = transform.position + Random.insideUnitSphere * 10f;
                patrolPoint.y = transform.position.y;
                if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, (patrolPoint - transform.position).normalized, 10f, obstacleLayer))
                {
                    if (currentTarget != null) Destroy(currentTarget);
                    currentTarget = new GameObject("PatrolPoint");
                    currentTarget.transform.position = patrolPoint;
                    break;
                }
            }
        }
    }

    private void HandleAssessing()
    {
        Collider[] windows = Physics.OverlapSphere(transform.position, detectionRange, windowLayer);
        entryPointStrengths.Clear();

        foreach (Collider window in windows)
        {
            float strength = EvaluateEntryPointStrength(window.gameObject);
            entryPointStrengths[window.gameObject] = strength;
        }

        var weakest = entryPointStrengths.OrderBy(x => x.Value).Select(x => x.Key).FirstOrDefault();
        if (weakest != null && weakest != currentTarget)
        {
            currentTarget = weakest;
            attackAttempts = 0;
        }

        currentState = Random.value < deceptionChance ? MonsterState.Deceiving : MonsterState.Attacking;
    }

    private void HandleDeceiving()
    {
        if (!isDeceiving) StartDeception();
        deceptionTimer -= Time.deltaTime;
        if (deceptionTimer <= 0)
        {
            isDeceiving = false;
            currentState = MonsterState.Assessing;
        }
    }

    private void HandleAttacking()
    {
        if (currentTarget == null)
        {
            currentState = MonsterState.Assessing;
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance < 2f && Time.time - lastAttackTime > attackCooldown)
        {
            AttemptBreakIn();
            lastAttackTime = Time.time;
            attackAttempts++;

            if (attackAttempts >= 3)
            {
                currentState = MonsterState.Assessing;
                currentTarget = null;
            }
        }
        else if (Time.time - lastJumpTime > jumpCooldown && CanJumpTo(currentTarget.transform.position))
        {
            jumpTargetPosition = currentTarget.transform.position;
            currentState = MonsterState.Jumping;
        }
    }

    private void HandleSearching() {}

    private void HandleJumping()
    {
        if (!isJumping)
        {
            isJumping = true;
            jumpStartTime = Time.time;
            jumpStartPosition = transform.position;
            lastJumpTime = Time.time;
        }

        float progress = (Time.time - jumpStartTime) / jumpDuration;
        if (progress <= 1f)
        {
            float height = jumpHeight * Mathf.Sin(progress * Mathf.PI);
            Vector3 newPos = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, progress);
            newPos.y += height;
            rb.MovePosition(newPos);
        }
        else
        {
            isJumping = false;
            currentState = MonsterState.Assessing;
        }
    }

    private bool CanJumpTo(Vector3 target)
    {
        float dist = Vector3.Distance(transform.position, target);
        if (dist > jumpDistance || Mathf.Abs(target.y - transform.position.y) > jumpHeight)
            return false;

        return !Physics.Raycast(transform.position + Vector3.up * 0.5f, (target - transform.position).normalized, dist, obstacleLayer);
    }

    private void UpdateDecision()
    {
        currentState = Vector3.Distance(transform.position, player.position) <= detectionRange ? MonsterState.Assessing : MonsterState.Patrolling;
    }

    private float EvaluateEntryPointStrength(GameObject entryPoint)
    {
        float strength = 0f;
        var boardingSystem = FindObjectOfType<BoardingSystem>();
        if (boardingSystem != null)
            strength += boardingSystem.GetWindowBoardCount(entryPoint) * 0.5f;

        Collider[] obstacles = Physics.OverlapSphere(entryPoint.transform.position, entryPointCheckRadius, obstacleLayer);
        strength += obstacles.Length * 0.3f;

        return strength;
    }

    private void StartDeception()
    {
        isDeceiving = true;
        deceptionTimer = Random.Range(minDeceptionDelay, maxDeceptionDelay);
        AudioClip[] sounds = Random.value < 0.5f ? tapSounds : creakSounds;
        if (sounds.Length > 0)
            audioSource.PlayOneShot(sounds[Random.Range(0, sounds.Length)]);
    }

    private void AttemptBreakIn()
    {
        Debug.Log("Attempting break-in at: " + currentTarget.name);
        BoardingSystem boardingSystem = FindObjectOfType<BoardingSystem>();
        if (boardingSystem != null)
        {
            boardingSystem.RemoveBoardFromWindow(currentTarget);
        }
    }

    private void OnDrawGizmos()
    {
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            Gizmos.DrawSphere(currentTarget.transform.position, 0.3f);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
