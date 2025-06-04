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

    private float lastSpawnZ = 0f;
    private float timer = 0f;
    private float startTime;
    private GameObject lastObstacle;

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        // Only spawn if no obstacle or the last one is behind the player
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
        // Increase spike chance over time
        float t = Mathf.Clamp01((Time.time - startTime) / spikeChanceRampTime);
        float spikeChance = Mathf.Lerp(spikeChanceStart, spikeChanceMax, t);
        bool spawnSpike = Random.value < spikeChance;

        // Pick a random lane
        int lane = Random.Range(0, lanePositions.Length);
        float x = lanePositions[lane];
        float y = planeY + Random.Range(-1f, 1f);
        float z = PlayerMovement.Instance.transform.position.z + spawnDistance;
        Vector3 spawnPos = new Vector3(x, y, z);

        GameObject prefab = spawnSpike ? spikePrefab : slopePrefab;
        lastObstacle = Instantiate(prefab, spawnPos, Quaternion.identity);

        // If it's a slope, randomize its scale
        if (!spawnSpike && lastObstacle != null)
        {
            float randomScale = Random.Range(5f, 20f);
            lastObstacle.transform.localScale = new Vector3(randomScale, lastObstacle.transform.localScale.y, lastObstacle.transform.localScale.z);
        }
    }
} 