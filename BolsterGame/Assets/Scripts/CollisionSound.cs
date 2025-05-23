using UnityEngine;

public class CollisionSound : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField] private AudioClip[] collisionSounds;
    [SerializeField] private float minVolume = 0.3f;
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;
    [SerializeField] private float minImpactForce = 2f;
    [SerializeField] private float maxImpactForce = 10f;
    [SerializeField] private float playerCheckRadius = 20f; // Only play sounds if player is within this radius

    private AudioSource audioSource;
    private Transform playerTransform;
    private float lastPlayTime;
    private float minTimeBetweenSounds = 0.1f; // Prevent sound spam
    private ObjectPickup objectPickup;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            objectPickup = player.GetComponent<ObjectPickup>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collisionSounds == null || collisionSounds.Length == 0) return;
        if (Time.time - lastPlayTime < minTimeBetweenSounds) return;

        // Check if player is within range
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > playerCheckRadius) return;
        }

        // Calculate impact force
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minImpactForce) return;

        // Calculate volume based on impact force
        float normalizedForce = Mathf.Clamp01((impactForce - minImpactForce) / (maxImpactForce - minImpactForce));
        float volume = Mathf.Lerp(minVolume, maxVolume, normalizedForce);

        // Randomize pitch and volume slightly
        float randomPitch = Random.Range(minPitch, maxPitch);
        float randomVolume = volume * Random.Range(0.9f, 1.1f);

        // Play random sound from array
        AudioClip randomSound = collisionSounds[Random.Range(0, collisionSounds.Length)];
        
        audioSource.pitch = randomPitch;
        audioSource.volume = randomVolume;
        audioSource.PlayOneShot(randomSound);

        lastPlayTime = Time.time;

        // Notify ObjectPickup system about the collision
        if (objectPickup != null)
        {
            objectPickup.OnObjectCollision();
        }
    }
} 