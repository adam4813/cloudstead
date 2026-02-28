using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameState currentState = GameState.Playing;

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
