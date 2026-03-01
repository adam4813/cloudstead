using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameState currentState = GameState.Playing;

    // TODO: move to ResearchManager when research system is implemented
    [Tooltip("Chebyshev tile radius the player can interact with (1 = adjacent tiles only)")]
    [SerializeField] private int playerInteractionRange = 1;
    public int PlayerInteractionRange => playerInteractionRange;

    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;

    public void SetState(GameState newState)
    {
        if (newState == currentState) return;

        var previous = currentState;
        currentState = newState;

        EventBus.Publish(new GameStateChangedEvent
        {
            Previous = previous,
            Current = newState
        });
    }
}
