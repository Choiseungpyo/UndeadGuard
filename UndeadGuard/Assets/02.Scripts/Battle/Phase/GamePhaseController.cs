using UnityEngine;

// 전투 페이즈와 정비 페이즈 전환을 관리한다
// 페이즈 이벤트만 발행하며 다른 시스템을 직접 호출하지 않는다
// 게임 흐름: 씬 시작 → 정비 → 전투 → (웨이브 클리어) → 정비 → 전투 ...
public class GamePhaseController : Singleton<GamePhaseController>
{
    private PhaseType currentPhase;

    public PhaseType CurrentPhase => currentPhase;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Subscribe<CoreDestroyedEvent>(OnCoreDestroyed);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Unsubscribe<CoreDestroyedEvent>(OnCoreDestroyed);
    }

    // 씬 로드 시 정비 페이즈 이벤트를 발행한다
    // 각 시스템은 이 이벤트를 구독해 독립적으로 초기화를 처리한다
    private void Start()
    {
        EnterPreparationPhase();
    }

    // 플레이어가 준비 완료 버튼을 누르면 호출한다
    public void RequestNextWave()
    {
        if (currentPhase != PhaseType.Preparation) return;

        EnterBattlePhase();
    }

    // 정비 페이즈 이벤트를 발행한다
    private void EnterPreparationPhase()
    {
        currentPhase = PhaseType.Preparation;
        EventBus.Instance.Publish(new PhaseChangedEvent { CurrentPhase = PhaseType.Preparation });
    }

    // 전투 페이즈 이벤트를 발행한다
    // 웨이브 시작과 플레이어 턴 시작은 각 구독 클래스에서 처리한다
    private void EnterBattlePhase()
    {
        currentPhase = PhaseType.Battle;
        EventBus.Instance.Publish(new PhaseChangedEvent { CurrentPhase = PhaseType.Battle });
    }

    // 마지막 웨이브 클리어 시에만 배치 단계로 전환한다
    // 중간 웨이브 클리어는 WaveManager가 다음 웨이브를 자동 시작하므로 여기서 처리하지 않는다
    private void OnWaveCleared(WaveClearedEvent e)
    {
        if (!WaveManager.Instance.IsLastWave) return;

        EnterPreparationPhase();
    }

    // 코어가 파괴되면 게임 오버 처리한다
    private void OnCoreDestroyed(CoreDestroyedEvent e)
    {
        Debug.Log("게임 오버: 언데드 핵이 파괴되었습니다.");
    }
}
