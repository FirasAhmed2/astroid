// PowerUpConfig — ScriptableObject that holds everything needed to define one power-up type.
// Create a preset asset for each power-up via Assets > Asteroid Dodger > Power Up Config,
// then load them at runtime using Resources.Load<PowerUpConfig>("PowerUps/PowerUpConfig_Shield") etc.

using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpConfig", menuName = "Asteroid Dodger/Power Up Config")]
public class PowerUpConfig : ScriptableObject
{
    // Identity
    [Header("Identity")]
    [Tooltip("Which power-up type this config represents.")]
    public PowerUpType powerUpType;

    [Tooltip("Short label shown to the player when this power-up is collected (e.g. 'SHIELD').")]
    public string displayName;

    // Visuals
    [Header("Visuals")]
    [Tooltip("Main color used for the power-up pickup object.")]
    public Color primaryColor = Color.white;

    [Tooltip("Glow/outline color layered on top of the primary color.")]
    public Color glowColor = Color.white;

    [Tooltip("Optional icon displayed in the HUD while the effect is active.")]
    public Sprite icon;

    // Tuning
    [Header("Tuning")]
    [Tooltip("How long the power-up effect lasts after the player collects it (seconds).")]
    public float effectDuration = 5f;

    [Tooltip("How long the pickup stays on screen before disappearing on its own (seconds).")]
    public float lifetime = 8f;

    [Tooltip("How fast the pickup falls down the screen (units per second).")]
    public float fallSpeed = 2f;
}
