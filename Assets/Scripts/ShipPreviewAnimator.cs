// ShipPreviewAnimator — makes the ship bob left and right on the customization screen.
// This keeps the TrailRenderer drawing continuously so the player can actually see
// the trail color they picked. Attach it to the ShipPreview GameObject.

using UnityEngine;

public class ShipPreviewAnimator : MonoBehaviour
{
    // Movement
    [Tooltip("How fast the ship oscillates left and right. 1.2 feels relaxed but lively.")]
    [SerializeField] private float speed = 1.2f;

    [Tooltip("How far left/right the ship travels in world units.")]
    [SerializeField] private float range = 1.8f;

    // References
    private Transform shipTransform;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        shipTransform = transform;
    }

    private void Update()
    {
        // Skip in the editor scene view — only animate during actual play
        if (!Application.isPlaying) return;

        float xOffset = Mathf.Sin(Time.time * speed) * range;

        // Only change X so the ship stays centered vertically
        Vector3 pos = shipTransform.localPosition;
        pos.x = xOffset;
        shipTransform.localPosition = pos;
    }
}
