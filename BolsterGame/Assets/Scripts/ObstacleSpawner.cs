using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject slopePrefab;
    public GameObject spikePrefab;
    public float spawnDistance = 30f;
    public float timeBetweenSpawns = 2f;
    public float spikeChanceStart = 0.1f; // Initial chance to spawn a spike
    public float spikeChanceMax = 0.8f;   // Max chance to spawn a spike
    public float spikeChanceRampTime = 60f; // Time in seconds to reach max chance
    public float[] lanePositions = new float[] { -5f, 0f, 5f };
    public float planeY = 0f; // Base y position for the plane

    // Reference objects for the desired rotation when spawning obstacles.
    // Assign these in the Inspector if you want the spawned object to have a specific angle.
    public Transform slopeReference;
    public Transform spikeReference;

    private float timer = 0f;
    private float startTime;
    private GameObject lastObstacle;

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        if (PlayerMovement.Instance == null)
            return;

        // Only spawn a new obstacle if there is none or the last one is behind the player.
        if (lastObstacle == null || lastObstacle.transform.position.z < PlayerMovement.Instance.transform.position.z - 2f)
        {
            timer += Time.deltaTime;
            if (timer >= timeBetweenSpawns)
            {
                SpawnObstacle();
                timer = 0f;
            }
        }
    }

    void SpawnObstacle()
    {
        // Ramp the spike chance over time.
        float t = Mathf.Clamp01((Time.time - startTime) / spikeChanceRampTime);
        float spikeChance = Mathf.Lerp(spikeChanceStart, spikeChanceMax, t);
        bool spawnSpike = Random.value < spikeChance;
    
        // Pick a random lane and compute the spawn position.
        int lane = Random.Range(0, lanePositions.Length);
        float x = lanePositions[lane];
        float y = planeY + Random.Range(-1f, 1f);
        float z = PlayerMovement.Instance.transform.position.z + spawnDistance;
        Vector3 spawnPos = new Vector3(x, y, z);
    
        GameObject prefab = spawnSpike ? spikePrefab : slopePrefab;
        
        // Use the reference object's rotation if it's set; otherwise, use the spawner's rotation.
        Quaternion spawnRotation;
        if (spawnSpike)
        {
            spawnRotation = spikeReference != null ? spikeReference.rotation : transform.rotation;
        }
        else
        {
            spawnRotation = slopeReference != null ? slopeReference.rotation : transform.rotation;
        }
    
        lastObstacle = Instantiate(prefab, spawnPos, spawnRotation);
    
        // For a slope, randomize its scale if desired.
        if (!spawnSpike && lastObstacle != null)
        {
            float randomScale = Random.Range(5f, 20f);
            lastObstacle.transform.localScale = new Vector3(randomScale, lastObstacle.transform.localScale.y, lastObstacle.transform.localScale.z);
        }
    }
}
