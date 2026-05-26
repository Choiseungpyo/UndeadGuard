using UnityEngine;

// Compatibility bridge for scenes/prefabs that still have AppFlowCoordinator attached.
// Scene flow now lives in GameManager.
public sealed class AppFlowCoordinator : MonoBehaviour
{
    public static AppFlowCoordinator Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private AppFlowSceneConfig sceneConfig;
    [SerializeField] private bool startInTitleOnBoot;

    public AppFlowState CurrentState => GameManager.Instance != null
        ? GameManager.Instance.CurrentState
        : AppFlowState.None;

    public bool IsTransitioning => GameManager.Instance != null && GameManager.Instance.IsTransitioning;

    public static bool TryGetExisting(out AppFlowCoordinator coordinator)
    {
        coordinator = Instance;
        return coordinator != null;
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
        EnsureGameManager();
    }

    private void Start()
    {
        EnsureGameManager();
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    private void EnsureGameManager()
    {
        GameManager manager = GameManager.Instance != null ? GameManager.Instance : GetComponent<GameManager>();
        if (manager == null)
            manager = gameObject.AddComponent<GameManager>();

        manager.ConfigureSceneFlow(sceneConfig, startInTitleOnBoot);
    }
}
