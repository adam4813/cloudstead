using UnityEngine;

/// <summary>
/// Simple visual house structure on the farm cloud.
/// Purely decorative for MVP — establishes the player's home presence.
/// Future: interior scene, upgrades, furniture placement.
/// </summary>
public class HouseStructure : MonoBehaviour
{
    [SerializeField] private SpriteRenderer houseRenderer;

    private void Awake()
    {
        if (houseRenderer == null)
            houseRenderer = GetComponent<SpriteRenderer>();
    }
}
