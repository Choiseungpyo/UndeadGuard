using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class TitleFlowEntry : MonoBehaviour
{
    [SerializeField] private AppFlowSceneConfig sceneConfig;
    [SerializeField] private bool detectStartInputInUpdate = true;
    [SerializeField] private bool ignoreEscapeKey = true;

    private bool isTransitioning;

    private void Update()
    {
        if (!detectStartInputInUpdate || isTransitioning)
            return;

        if (!IsStartInputDetected())
            return;

        OnStartButtonPressed();
    }

    public void OnStartButtonPressed()
    {
        if (isTransitioning)
            return;

        string sceneName = sceneConfig != null ? sceneConfig.IntroCutsceneSceneName : string.Empty;
        TransitionToScene(
            new RequestPlayIntroCutsceneEvent { SceneName = sceneName },
            sceneName,
            "[TitleFlowEntry] IntroCutscene scene name is empty or not loadable."
        );
    }

    public void OnSkipToLobbyButtonPressed()
    {
        if (isTransitioning)
            return;

        string sceneName = sceneConfig != null ? sceneConfig.LobbySceneName : string.Empty;
        TransitionToScene(
            new RequestOpenLobbyEvent { SceneName = sceneName },
            sceneName,
            "[TitleFlowEntry] Lobby scene name is empty or not loadable."
        );
    }

    public void OnSettingsButtonPressed()
    {
        if (sceneConfig == null || string.IsNullOrWhiteSpace(sceneConfig.SettingsSceneName))
        {
            Debug.Log("Settings is not implemented yet.");
            return;
        }

        EventBus.Instance.Publish(new RequestOpenSettingsEvent
        {
            SceneName = sceneConfig.SettingsSceneName
        });
    }

    private void TransitionToScene<T>(T requestEvent, string sceneName, string invalidSceneWarning)
    {
        isTransitioning = true;

        // Primary path: GameManager via EventBus.
        EventBus.Instance.Publish(requestEvent);

        // Fallback path: when playing Title scene directly without GameManager.
        if (GameManager.Instance == null)
        {
            if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                return;
            }

            Debug.LogWarning($"{invalidSceneWarning} - sceneName : {sceneName}");
            isTransitioning = false;
            return;
        }

        // If coordinator exists but transition did not start (e.g. bad scene name), unlock input.
        if (!GameManager.Instance.IsTransitioning)
        {
            Debug.LogWarning(invalidSceneWarning);
            isTransitioning = false;
        }
    }

    private bool IsStartInputDetected()
    {
#if ENABLE_INPUT_SYSTEM
        bool mouseClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        bool keyboardInput = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        if (ignoreEscapeKey && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            keyboardInput = false;

        return mouseClick || keyboardInput;
#else
        bool mouseClick = Input.GetMouseButtonDown(0);
        bool keyboardInput = Input.anyKeyDown;
        if (ignoreEscapeKey && Input.GetKeyDown(KeyCode.Escape))
            keyboardInput = false;

        return mouseClick || keyboardInput;
#endif
    }
}
