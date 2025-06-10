using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("References")]
    public Transform player;         // Assign the player object in the Inspector
    public TMP_Text scoreText;       // Assign the TextMeshPro UI component

    [Header("Score Settings")]
    private float score;
    private int obstaclesDodged;
    private float timeInAir;
    private float targetScore;

    [Header("Text Effect Settings")]
    private float textSize = 36f;        // Default text size
    private float targetTextSize;
    private Color defaultColor;          // The default color for the score text
    private Color bonusColor = new Color(1f, 0.8f, 0f); // Orange-yellow tint

    // Bonus effect timing
    private float bonusDuration = 0.5f;
    private float bonusEndTime = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // initialize text scale and color
        targetTextSize = textSize;
        scoreText.fontSize = textSize;
        defaultColor = scoreText.color;  // store the starting/default color
    }

    void Update()
    {
        CalculateScore();
        UpdateScoreUI();
        UpdateBonusEffects();
    }

    void CalculateScore()
    {
        // Calculate score based on player's z position, obstacles, and air time.
        score = player.position.z + (obstaclesDodged * 1000) + (timeInAir * 300);

        // Smoothly interpolate the displayed score
        targetScore = Mathf.Lerp(targetScore, score, Time.deltaTime * 5f);

        // Smoothly interpolate font size to achieve a "bump" effect on bonus
        scoreText.fontSize = Mathf.Lerp(scoreText.fontSize, targetTextSize, Time.deltaTime * 5f);
    }

    void UpdateScoreUI()
    {
        // Update the text to display the rounded score.
        scoreText.text = $"Score: {Mathf.RoundToInt(targetScore)}";
    }

    void UpdateBonusEffects()
    {
        // If we're still within the bonus time window, apply rotation and bonus color.
        if (Time.time < bonusEndTime)
        {
            float oscillationAngle = Mathf.Sin(Time.time * 20f) * 20f; // oscillates between -20 and 20 degrees
            scoreText.transform.rotation = Quaternion.Euler(0, 0, oscillationAngle);
        }
        else
        {
            // Reset rotation and color once bonus effect is done.
            scoreText.transform.rotation = Quaternion.identity;
            scoreText.color = defaultColor;
            // Also ensure that the text size target goes back to normal.
            targetTextSize = textSize;
        }
    }

    /// <summary>
    /// Call this method when the player passes an obstacle.
    /// </summary>
    public void AddObstacleBonus()
    {
        obstaclesDodged++;
        TriggerBonusEffect();
    }

    /// <summary>
    /// Call this method when the player is in the air.
    /// You might call it each frame the player is airborne.
    /// </summary>
    /// <param name="airTimeAmount">Typically Time.deltaTime, or the total air time on landing.</param>
    public void AddAirTimeBonus(float airTimeAmount)
    {
        timeInAir += airTimeAmount;
        TriggerBonusEffect();
    }

    /// <summary>
    /// Triggers the bonus visual effects: scaling, color change, and rotation.
    /// </summary>
    void TriggerBonusEffect()
    {
        // Increase target text size for a "bump" effect.
        targetTextSize = textSize * 1.2f;
        // Change text color to bonus color.
        scoreText.color = bonusColor;
        // Set the bonus time window to now + bonusDuration.
        bonusEndTime = Time.time + bonusDuration;
    }
}
