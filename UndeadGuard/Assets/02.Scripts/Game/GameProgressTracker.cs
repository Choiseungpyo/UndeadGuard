using UnityEngine;

// 게임 진행 정보를 중앙에서 관리하고 UI에 전달한다
// 전투판 로직(TurnManager, WaveManager)과 분리되어 진행 상태만 추적한다
//
// 관리 항목
//   현재 일차 (n일차)
//   현재 단계 (배치 / 전투)
//   전투 단계에서의 Phase 번호와 전체 Phase 수
//   전투 단계에서의 턴 주체와 현재 라운드
//
// 규칙
//   배치 단계에서는 Phase, 턴 관련 값을 발행하지 않는다
//   일차는 전투를 완료하고 다음 배치 단계에 진입할 때 증가한다
//   게임 시작 첫 배치 단계는 일차 증가 없이 1일차로 시작한다
public class GameProgressTracker : Singleton<GameProgressTracker>
{
    // 현재 일차 (1부터 시작)
    private int currentDay = 1;

    // 현재 단계
    private PhaseType currentStage = PhaseType.Preparation;

    // 전투 단계에서의 현재 Phase 번호 (WaveNumber 기반, 0이면 미시작)
    private int currentPhaseIndex;

    // 전투 단계에서의 전체 Phase 수
    private int totalPhases;

    // 현재 턴 주체
    private TurnType currentTurn;

    // 현재 웨이브 내 라운드 번호
    private int currentRound;

    // 게임 시작 이후 전투가 한 번이라도 완료되었는지 여부
    // false이면 다음 배치 단계 진입 시 일차를 증가시키지 않는다
    private bool hasBattleCompleted;

    public int CurrentDay => currentDay;
    public PhaseType CurrentStage => currentStage;
    public int CurrentPhaseIndex => currentPhaseIndex;
    public int TotalPhases => totalPhases;
    public TurnType CurrentTurn => currentTurn;
    public int CurrentRound => currentRound;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
        EventBus.Instance.Subscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Subscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
        EventBus.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Unsubscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
    }

    // 단계 전환 처리
    // 배치 단계: 전투 완료 후 진입 시 일차 증가, Phase/턴 정보 초기화
    // 전투 단계: Phase 정보는 WaveStartedEvent에서 설정
    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (e.CurrentPhase == PhaseType.Preparation)
        {
            if (hasBattleCompleted)
            {
                currentDay++;
                hasBattleCompleted = false;
            }

            currentStage = PhaseType.Preparation;
            currentPhaseIndex = 0;
            totalPhases = 0;
            currentRound = 0;
        }
        else if (e.CurrentPhase == PhaseType.Battle)
        {
            currentStage = PhaseType.Battle;
        }

        PublishUpdate();
    }

    // 웨이브(Phase) 시작: Phase 번호와 전체 수 갱신, 라운드 초기화
    private void OnWaveStarted(WaveStartedEvent e)
    {
        currentPhaseIndex = e.WaveNumber;
        totalPhases = e.TotalWaves;
        currentRound = 0;
        PublishUpdate();
    }

    // 마지막 웨이브 클리어: 전투 완료 플래그 설정 (다음 배치 단계 진입 시 일차 증가)
    private void OnWaveCleared(WaveClearedEvent e)
    {
        if (WaveManager.Instance.IsLastWave)
            hasBattleCompleted = true;
    }

    // 턴 변경: 플레이어 턴 시작 시 라운드 증가
    private void OnTurnChanged(TurnChangedEvent e)
    {
        currentTurn = e.CurrentTurn;

        if (e.CurrentTurn == TurnType.Player)
            currentRound++;

        PublishUpdate();
    }

    // 현재 진행 정보를 UI 표시용 데이터로 변환해 발행한다
    private void PublishUpdate()
    {
        bool isBattle = currentStage == PhaseType.Battle;
        bool phaseStarted = isBattle && currentPhaseIndex > 0;
        bool turnActive = isBattle && currentRound > 0;

        string phaseText = phaseStarted
            ? $"Phase {currentPhaseIndex} / {totalPhases}"
            : string.Empty;

        string turnText = string.Empty;
        if (turnActive)
        {
            turnText = currentTurn == TurnType.Player
                ? $"플레이어 턴 {currentRound}"
                : "적 턴";
        }

        EventBus.Instance.Publish(new GameProgressUpdatedEvent
        {
            DayText = $"{currentDay}일차",
            StageText = isBattle ? "전투 단계" : "배치 단계",
            PhaseText = phaseText,
            TurnText = turnText,
            ShowPhaseText = phaseStarted,
            ShowTurnText = turnActive,
            IsPreparationStage = !isBattle
        });
    }
}
