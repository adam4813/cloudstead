using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : Singleton<SceneTransitionManager>
{
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool _isTransitioning;

    public override void Initialize()
    {
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
        }
    }

    public void LoadScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionCoroutine(sceneName, null));
    }

    public void LoadSceneFromSave(string slotName, string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionCoroutine(sceneName, slotName));
    }

    public void StartNewGame(string sceneName = "Farm")
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionCoroutine(sceneName, null));
    }

    private IEnumerator TransitionCoroutine(string sceneName, string saveSlot)
    {
        _isTransitioning = true;

        EventBus.Publish(new SceneTransitionStartedEvent { TargetScene = sceneName });

        // Fade out
        if (fadePanel != null)
        {
            fadePanel.blocksRaycasts = true;
            yield return StartCoroutine(Fade(0f, 1f));
        }

        Time.timeScale = 1f;

        // Load scene async
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;

        // Load save data after scene is ready
        if (saveSlot != null && SaveManager.Instance != null)
            SaveManager.Instance.Load(saveSlot);

        // Fade in
        if (fadePanel != null)
        {
            yield return StartCoroutine(Fade(1f, 0f));
            fadePanel.blocksRaycasts = false;
        }

        _isTransitioning = false;

        EventBus.Publish(new SceneTransitionCompletedEvent { Scene = sceneName });
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        fadePanel.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = to;
    }
}
