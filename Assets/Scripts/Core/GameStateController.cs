using UnityEngine;
using UnityEngine.InputSystem;

public class GameStateController : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    private void Start()
    {
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

        if (playerInput == null)
            playerInput = FindFirstObjectByType<PlayerInput>();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (playerInput == null) return;

        switch (evt.Current)
        {
            case GameState.Playing:
                playerInput.SwitchCurrentActionMap("Player");
                Time.timeScale = 1f;
                break;

            case GameState.Menu:
            case GameState.Dialogue:
                playerInput.SwitchCurrentActionMap("UI");
                Time.timeScale = 1f;
                break;

            case GameState.Paused:
                playerInput.SwitchCurrentActionMap("UI");
                Time.timeScale = 0f;
                break;

            case GameState.Sleeping:
                playerInput.enabled = false;
                playerInput.enabled = true; // Re-enable will reset
                break;

            case GameState.Airship:
                playerInput.SwitchCurrentActionMap("Airship");
                Time.timeScale = 1f;
                break;
        }
    }
}
