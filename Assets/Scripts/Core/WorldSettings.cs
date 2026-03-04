using UnityEngine;

/// <summary>
/// Serializable world/game options that persist to the save file.
/// Configured via pause menu settings; NOT stored in PlayerPrefs.
/// </summary>
[System.Serializable]
public class WorldSettings
{
    [Header("Time")]
    [Tooltip("When true, time pauses during menus and dialogue")]
    public bool pauseTimeInMenus = true;

    [Tooltip("Day length in real-time seconds (default 720 = 12 min)")]
    [Min(60f)]
    public float dayLengthSeconds = 720f;

    [Header("Gameplay")]
    [Tooltip("Chebyshev tile radius the player can interact with (1 = adjacent tiles only)")]
    [Range(1, 3)]
    public int playerInteractionRange = 1;

    [Header("Narrative")]
    [Tooltip("True during a new game until the opening narrative completes (Day 2 shipyard handoff)")]
    public bool isNewGame;

    public WorldSettings Clone()
    {
        return (WorldSettings)MemberwiseClone();
    }
}
