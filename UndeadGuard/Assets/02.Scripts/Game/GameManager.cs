using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Title부터 살아있는 앱 흐름 관리자.
// 씬 전환은 여기서 처리하고, 배틀 한 판의 진행 시스템은 BattleGameRuntime에 맡긴다.
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Flow")]
    [SerializeField] private AppFlowSceneConfig sceneConfig;
    [SerializeField] private bool startInTitleOnBoot;

    private SceneTransitionService transitionService;
    private BattleGameRuntime battleRuntime;
    private Coroutine pendingBattleStartRoutine;
    private AppFlowState currentState = AppFlowState.None;

    public AppFlowState CurrentState => currentState;
    public bool IsTransitioning => transitionService != null && transitionService.IsTransitioning;

    public static bool TryGetExisting(out GameManager manager)
    {
        manager = Instance;
        return manager != null;
    }

    public void ConfigureSceneFlow(AppFlowSceneConfig config, bool startInTitle)
    {
        if (sceneConfig == null && config != null)
            sceneConfig = config;

        startInTitleOnBoot = startInTitleOnBoot || startInTitle;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        transitionService = new SceneTransitionService(this);
    }

    private void OnEnable()
    {
        if (!ReferenceEquals(Instance, this))
            return;

        EventBus.Instance.Subscribe<RequestOpenTitleEvent>(OnRequestOpenTitle);
        EventBus.Instance.Subscribe<RequestPlayIntroCutsceneEvent>(OnRequestPlayIntroCutscene);
        EventBus.Instance.Subscribe<RequestOpenLobbyEvent>(OnRequestOpenLobby);
        EventBus.Instance.Subscribe<RequestStartBattleEvent>(OnRequestStartBattle);
        EventBus.Instance.Subscribe<RequestRestartBattleEvent>(OnRequestRestartBattle);
        EventBus.Instance.Subscribe<RequestOpenResultEvent>(OnRequestOpenResult);
        EventBus.Instance.Subscribe<RequestOpenSettingsEvent>(OnRequestOpenSettings);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (startInTitleOnBoot)
        {
            RequestTransition(AppFlowState.Title, string.Empty);
            return;
        }

        SetCurrentStateByScene(SceneManager.GetActiveScene().name);
        if (currentState == AppFlowState.Battle)
            QueueBattleRuntimeStart();
    }

    private void OnDisable()
    {
        if (!ReferenceEquals(Instance, this))
            return;

        EventBus.Instance.Unsubscribe<RequestOpenTitleEvent>(OnRequestOpenTitle);
        EventBus.Instance.Unsubscribe<RequestPlayIntroCutsceneEvent>(OnRequestPlayIntroCutscene);
        EventBus.Instance.Unsubscribe<RequestOpenLobbyEvent>(OnRequestOpenLobby);
        EventBus.Instance.Unsubscribe<RequestStartBattleEvent>(OnRequestStartBattle);
        EventBus.Instance.Unsubscribe<RequestRestartBattleEvent>(OnRequestRestartBattle);
        EventBus.Instance.Unsubscribe<RequestOpenResultEvent>(OnRequestOpenResult);
        EventBus.Instance.Unsubscribe<RequestOpenSettingsEvent>(OnRequestOpenSettings);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        StopBattleRuntime();

        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetCurrentStateByScene(scene.name);

        if (currentState == AppFlowState.Battle)
            QueueBattleRuntimeStart();
        else
            StopBattleRuntime();
    }

    private void OnRequestOpenTitle(RequestOpenTitleEvent e)
    {
        RequestTransition(AppFlowState.Title, e.SceneName);
    }

    private void OnRequestPlayIntroCutscene(RequestPlayIntroCutsceneEvent e)
    {
        RequestTransition(AppFlowState.Cutscene, e.SceneName);
    }

    private void OnRequestOpenLobby(RequestOpenLobbyEvent e)
    {
        RequestTransition(AppFlowState.Lobby, e.SceneName);
    }

    private void OnRequestStartBattle(RequestStartBattleEvent e)
    {
        RequestTransition(AppFlowState.Battle, e.SceneName);
    }

    private void OnRequestRestartBattle(RequestRestartBattleEvent e)
    {
        RequestTransition(AppFlowState.Battle, e.SceneName);
    }

    private void OnRequestOpenResult(RequestOpenResultEvent e)
    {
        RequestTransition(AppFlowState.Result, e.SceneName);
    }

    private void OnRequestOpenSettings(RequestOpenSettingsEvent e)
    {
        RequestTransition(AppFlowState.Settings, e.SceneName);
    }

    private void RequestTransition(AppFlowState targetState, string sceneNameOverride)
    {
        string configuredSceneName = sceneConfig != null ? sceneConfig.ResolveSceneName(targetState) : string.Empty;
        string sceneName = string.IsNullOrWhiteSpace(sceneNameOverride) ? configuredSceneName : sceneNameOverride;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"[GameManager] Scene name is empty for state {targetState}.");
            return;
        }

        if (!transitionService.TryTransition(sceneName))
            return;

        currentState = targetState;
    }

    private void SetCurrentStateByScene(string sceneName)
    {
        currentState = ResolveStateByScene(sceneName);
    }

    private AppFlowState ResolveStateByScene(string sceneName)
    {
        if (sceneConfig == null)
            return AppFlowState.None;

        if (string.Equals(sceneName, sceneConfig.TitleSceneName, System.StringComparison.Ordinal))
            return AppFlowState.Title;
        if (string.Equals(sceneName, sceneConfig.IntroCutsceneSceneName, System.StringComparison.Ordinal))
            return AppFlowState.Cutscene;
        if (string.Equals(sceneName, sceneConfig.LobbySceneName, System.StringComparison.Ordinal))
            return AppFlowState.Lobby;
        if (string.Equals(sceneName, sceneConfig.BattleSceneName, System.StringComparison.Ordinal))
            return AppFlowState.Battle;
        if (string.Equals(sceneName, sceneConfig.ResultSceneName, System.StringComparison.Ordinal))
            return AppFlowState.Result;
        if (string.Equals(sceneName, sceneConfig.SettingsSceneName, System.StringComparison.Ordinal))
            return AppFlowState.Settings;

        return AppFlowState.None;
    }

    private void QueueBattleRuntimeStart()
    {
        if (pendingBattleStartRoutine != null)
            StopCoroutine(pendingBattleStartRoutine);

        pendingBattleStartRoutine = StartCoroutine(StartBattleRuntimeAfterSceneSettles());
    }

    private IEnumerator StartBattleRuntimeAfterSceneSettles()
    {
        yield return null;
        pendingBattleStartRoutine = null;

        StopBattleRuntime();
        battleRuntime = new BattleGameRuntime();
        battleRuntime.Begin();
    }

    private void StopBattleRuntime()
    {
        if (pendingBattleStartRoutine != null)
        {
            StopCoroutine(pendingBattleStartRoutine);
            pendingBattleStartRoutine = null;
        }

        if (battleRuntime == null)
            return;

        battleRuntime.Cleanup();
        battleRuntime = null;
    }
}
