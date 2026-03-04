using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI saveConfirmationText;

    private Coroutine _saveConfirmationCoroutine;

    private void Awake()
    {
        menuPanel.SetActive(false);
        if (saveConfirmationText != null) saveConfirmationText.gameObject.SetActive(false);

        resumeButton.onClick.AddListener(OnResume);
        saveButton.onClick.AddListener(OnSave);
        quitButton.onClick.AddListener(OnQuit);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private GameState _stateBeforePause;

    private void TogglePause()
    {
        var state = GameManager.Instance.CurrentState;

        if (state == GameState.Playing || state == GameState.Airship)
        {
            _stateBeforePause = state;
            GameManager.Instance.SetState(GameState.Paused);
            menuPanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else if (state == GameState.Paused)
        {
            Resume();
        }
    }

    private void Resume()
    {
        Time.timeScale = 1f;
        menuPanel.SetActive(false);
        GameManager.Instance.SetState(_stateBeforePause);
    }

    private void OnResume()
    {
        TogglePause();
    }

    private void OnSave()
    {
        SaveManager.Instance.Save(SaveManager.Instance.ActiveSlot);

        if (saveConfirmationText != null)
        {
            if (_saveConfirmationCoroutine != null) StopCoroutine(_saveConfirmationCoroutine);
            _saveConfirmationCoroutine = StartCoroutine(ShowSaveConfirmation());
        }
    }

    private IEnumerator ShowSaveConfirmation()
    {
        saveConfirmationText.text = "Game Saved!";
        saveConfirmationText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        saveConfirmationText.gameObject.SetActive(false);
        _saveConfirmationCoroutine = null;
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.Current == GameState.Paused)
        {
            menuPanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else if (evt.Previous == GameState.Paused)
        {
            menuPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    private void OnDestroy()
    {
        resumeButton.onClick.RemoveListener(OnResume);
        saveButton.onClick.RemoveListener(OnSave);
        quitButton.onClick.RemoveListener(OnQuit);
    }
}
