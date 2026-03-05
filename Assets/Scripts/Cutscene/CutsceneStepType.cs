public enum CutsceneStepType
{
    /// <summary>Smoothly walks a named actor to a world position.</summary>
    MoveActor,

    /// <summary>Instantly teleports a named actor to a world position.</summary>
    TeleportActor,

    /// <summary>Points the camera at a named actor (or world position if no actor given).</summary>
    FocusCamera,

    /// <summary>Returns the camera to the player.</summary>
    ReturnCamera,

    /// <summary>Opens a DialogueTree and waits for the player to dismiss it.</summary>
    ShowDialogue,

    /// <summary>Pauses the sequence for a fixed number of seconds.</summary>
    Wait,

    /// <summary>Boards the player (or another actor) onto an airship as a passenger. No pilot input.</summary>
    BoardAirship,

    /// <summary>Disembarks the player from an airship at a world position.</summary>
    DisembarkAirship,

    /// <summary>Shows or hides a named actor's GameObject.</summary>
    SetActorActive,

    /// <summary>Sets the ownerId on a named AirshipController component.</summary>
    TransferOwnership,
}
