using UnityEngine;

// 준비 단계와 전투 단계 전환을 관리한다
// 단계 이벤트만 발행하며 다른 시스템을 직접 호출하지 않는다
// 게임 흐름: 씬 시작 → 준비 → 전투 → (웨이브 클리어) → 준비 → 전투 ...
// BattleGameRuntime이 생성하며, 하이라키 오브젝트를 필요로 하지 않는다
public class GameStageController
{
    public static GameStageController Instance { get; private set; }

    private StageType currentStage;
    private bool isCleanedUp;
    private bool enemyTurnActive;
    private bool pendingWaveClearRewardFinished;
    private WaveClearedEvent pendingWaveClearedEvent;
    public StageType CurrentStage => currentStage;

    public GameStageController()
    {
        if (Instance != null && !ReferenceEquals(Instance, this))
        {
            Debug.LogWarning("[GameStageController] Existing instance detected. Cleaning up previous instance before replacement.");
            Instance.Cleanup();
        }

        Instance = this;
        isCleanedUp = false;
        EventBus.Instance.Subscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Subscribe<WaveClearRewardFinishedEvent>(OnWaveClearRewardFinished);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<ActionCameraFlowFinishedEvent>(OnActionCameraFlowFinished);
        EventBus.Instance.Subscribe<EnemyTurnFinishedEvent>(OnEnemyTurnFinished);
        EventBus.Instance.Subscribe<CoreDestroyedEvent>(OnCoreDestroyed);
    }

    // BattleGameRuntime.Begin에서 호출한다
    // 모든 MonoBehaviour의 Awake 완료 후 실행되어야 구독자들이 이벤트를 받을 수 있다
    public void Begin()
    {
        EnterPreparationStage();
    }

    public void Cleanup()
    {
        if (isCleanedUp)
            return;

        isCleanedUp = true;
        EventBus.Instance.Unsubscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Unsubscribe<WaveClearRewardFinishedEvent>(OnWaveClearRewardFinished);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<ActionCameraFlowFinishedEvent>(OnActionCameraFlowFinished);
        EventBus.Instance.Unsubscribe<EnemyTurnFinishedEvent>(OnEnemyTurnFinished);
        EventBus.Instance.Unsubscribe<CoreDestroyedEvent>(OnCoreDestroyed);

        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    // 플레이어가 준비 완료 버튼을 누르면 호출한다
    public void RequestNextWave()
    {
        if (currentStage != StageType.Preparation) return;

        EnterBattleStage();
    }

    private void EnterPreparationStage()
    {
        currentStage = StageType.Preparation;
        EventBus.Instance.Publish(new StageChangedEvent { CurrentStage = StageType.Preparation });
    }

    private void EnterBattleStage()
    {
        currentStage = StageType.Battle;
        EventBus.Instance.Publish(new StageChangedEvent { CurrentStage = StageType.Battle });
    }

    // 중간 웨이브 클리어 시에는 정비 단계로 돌아가고, 마지막 웨이브 클리어 시에는 승리를 알린다
    private void OnWaveCleared(WaveClearedEvent e)
    {
        pendingWaveClearedEvent = e;
        pendingWaveClearRewardFinished = false;
    }

    private void OnWaveClearRewardFinished(WaveClearRewardFinishedEvent e)
    {
        pendingWaveClearRewardFinished = true;
        TryResolvePendingWaveClear();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        enemyTurnActive = e.CurrentTurn == TurnType.Enemy;
    }

    private void OnActionCameraFlowFinished(ActionCameraFlowFinishedEvent e)
    {
        TryResolvePendingWaveClear();
    }

    private void OnEnemyTurnFinished(EnemyTurnFinishedEvent e)
    {
        enemyTurnActive = false;
        TryResolvePendingWaveClear();
    }

    private void TryResolvePendingWaveClear()
    {
        if (pendingWaveClearedEvent == null || !pendingWaveClearRewardFinished || ShouldDeferWaveClearTransition())
            return;

        WaveClearedEvent pending = pendingWaveClearedEvent;
        pendingWaveClearedEvent = null;
        pendingWaveClearRewardFinished = false;
        ResolveWaveCleared(pending);
    }

    private bool ShouldDeferWaveClearTransition()
    {
        if (currentStage != StageType.Battle)
            return false;

        if (enemyTurnActive)
            return true;

        return BattleInputGuard.TryGetExisting(out BattleInputGuard guard) && guard.IsActionCameraActive();
    }

    private void ResolveWaveCleared(WaveClearedEvent e)
    {
        if (!WaveManager.Instance.IsLastWave)
        {
            EnterPreparationStage();
            return;
        }

        EventBus.Instance.Publish(new BattleWonEvent
        {
            ClearedWaveNumber = e != null ? e.WaveNumber : WaveManager.Instance.CurrentWaveNumber,
            TotalWaves = WaveManager.Instance.TotalWaves
        });

        EventBus.Instance.Publish(new VictoryEvent
        {
            ClearedWaveNumber = e != null ? e.WaveNumber : WaveManager.Instance.CurrentWaveNumber,
            TotalWaves = WaveManager.Instance.TotalWaves
        });
    }

    private void OnCoreDestroyed(CoreDestroyedEvent e)
    {
        Debug.Log("게임 오버: 언데드 핵이 파괴되었습니다.");
    }
}
