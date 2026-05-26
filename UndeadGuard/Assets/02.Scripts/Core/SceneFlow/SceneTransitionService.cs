using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneTransitionService
{
    private readonly MonoBehaviour runner;
    private bool isTransitioning;

    public bool IsTransitioning => isTransitioning;

    public SceneTransitionService(MonoBehaviour runner)
    {
        this.runner = runner;
    }

    public bool TryTransition(string sceneName, Action onCompleted = null)
    {
        if (isTransitioning)
            return false;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneTransitionService] Scene name is empty.");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneTransitionService] Scene '{sceneName}' is not available in Build Settings.");
            return false;
        }

        runner.StartCoroutine(LoadSceneRoutine(sceneName, onCompleted));
        return true;
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action onCompleted)
    {
        isTransitioning = true;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        if (op == null)
        {
            isTransitioning = false;
            Debug.LogWarning($"[SceneTransitionService] Failed to start loading scene '{sceneName}'.");
            yield break;
        }

        while (!op.isDone)
            yield return null;

        isTransitioning = false;
        onCompleted?.Invoke();
    }
}
