// 게임 진행 수치를 중앙에서 보관하는 데이터 클래스
// 이벤트를 수신해 값을 갱신하고, 다른 시스템이 읽을 수 있도록 프로퍼티를 노출한다
// 페이즈 상태는 GameStageController가 관리하므로 이 클래스는 보관하지 않는다
// BattleGameRuntime이 생성하며, 하이라키 오브젝트를 필요로 하지 않는다
//
// 보관 항목
//   현재 일차 (n일차)
//   전투 단계에서의 Phase 번호와 전체 Phase 수
//   전투 단계에서의 턴 주체와 현재 라운드
public class GameProgress
{
    public static GameProgress Instance { get; private set; }

    private int currentDay = 1;
    private int currentPhaseIndex;
    private int totalPhases;
    private TurnType currentTurn;
    private int currentRound;

    // 게임 시작 이후 전투가 한 번이라도 완료되었는지 여부
    // false이면 다음 배치 단계 진입 시 일차를 증가시키지 않는다
    private bool hasBattleCompleted;
    private bool isCleanedUp;

    public int CurrentDay => currentDay;
    public int CurrentPhaseIndex => currentPhaseIndex;
    public int TotalPhases => totalPhases;
    public TurnType CurrentTurn => currentTurn;
    public int CurrentRound => currentRound;

    public GameProgress()
    {
        if (Instance != null && !object.ReferenceEquals(Instance, this))
            Instance.Cleanup();

        Instance = this;
        isCleanedUp = false;
        EventBus.Instance.Subscribe<StageChangedEvent>(OnStageChanged);
        EventBus.Instance.Subscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Subscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
    }

    public void Cleanup()
    {
        if (isCleanedUp)
            return;

        isCleanedUp = true;
        EventBus.Instance.Unsubscribe<StageChangedEvent>(OnStageChanged);
        EventBus.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Unsubscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);

        if (object.ReferenceEquals(Instance, this))
            Instance = null;
    }

    // 배치 단계 진입 시 전투 완료 플래그를 확인해 일차를 증가시키고 수치를 초기화한다
    private void OnStageChanged(StageChangedEvent e)
    {
        if (e.CurrentStage != StageType.Preparation) return;

        if (hasBattleCompleted)
        {
            currentDay++;
            hasBattleCompleted = false;
        }

        currentPhaseIndex = 0;
        totalPhases = 0;
        currentRound = 0;
    }

    // 웨이브 시작 시 Phase 번호와 전체 수를 갱신하고 라운드를 초기화한다
    private void OnWaveStarted(WaveStartedEvent e)
    {
        currentPhaseIndex = e.WaveNumber;
        totalPhases = e.TotalWaves;
        currentRound = 0;
    }

    // 마지막 웨이브 클리어 시 전투 완료 플래그를 설정한다
    private void OnWaveCleared(WaveClearedEvent e)
    {
        if (WaveManager.Instance.IsLastWave)
            hasBattleCompleted = true;
    }

    // 턴 변경 시 턴 주체를 저장하고, 플레이어 턴 시작 시 라운드를 증가시킨다
    private void OnTurnChanged(TurnChangedEvent e)
    {
        currentTurn = e.CurrentTurn;

        if (e.CurrentTurn == TurnType.Player)
            currentRound++;
    }
}
