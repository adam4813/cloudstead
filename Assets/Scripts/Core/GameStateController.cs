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
                playerInput.enabled = true;
                SwitchMapIfNeeded("Player");
                Time.timeScale = 1f;
                break;

            case GameState.Menu:
            case GameState.Dialogue:
                // Don't switch action maps — stay on Player map so
                // OpenInventory (Tab) and Interact (E) remain available.
                // Individual scripts check GameState to ignore input.
                Time.timeScale = 1f;
                break;

            case GameState.Paused:
                SwitchMapIfNeeded("UI");
                Time.timeScale = 0f;
                break;

            case GameState.Sleeping:
                playerInput.enabled = false;
                playerInput.enabled = true;
                break;

            case GameState.Cutscene:
                // Disable all input during cutscenes — CutscenePlayer controls everything
                playerInput.enabled = false;
                break;

            case GameState.Airship:
                SwitchMapIfNeeded("Airship");
                Time.timeScale = 1f;
                break;

            case GameState.Building:
                SwitchMapIfNeeded("Player");
                Time.timeScale = 1f;
                break;
        }
    }

    private void SwitchMapIfNeeded(string mapName)
    {
        if (!playerInput.enabled) return;
        if (playerInput.currentActionMap?.name != mapName)
            playerInput.SwitchCurrentActionMap(mapName);
    }
}
