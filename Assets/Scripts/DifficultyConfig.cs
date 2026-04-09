// DifficultyConfig — a ScriptableObject that holds all the tuning values for one
// difficulty level. Drop one of the preset assets onto a GameManager and the whole
// game adjusts itself automatically. Easy to add new presets without touching any code.

using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Asteroid Dodger/Difficulty Config")]
public class DifficultyConfig : ScriptableObject
{
    // Identity
    [Header("Identity")]
    [Tooltip("Human-readable name shown in the UI (e.g. 'Easy', 'Hard').")]
    public string difficultyName;

    // Asteroid
    [Header("Asteroid")]
    [Tooltip("How fast asteroids travel across the screen (units per second).")]
    public float asteroidSpeed;

    [Tooltip("Seconds between each new asteroid spawning. Lower = more chaos.")]
    public float spawnInterval;

    // Player
    [Header("Player")]
    [Tooltip("How fast the player ship moves (units per second).")]
    public float playerSpeed;
}
