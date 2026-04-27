using UnityEngine;

// 준비 단계와 전투 단계 전환을 관리한다
// 단계 이벤트만 발행하며 다른 시스템을 직접 호출하지 않는다
// 게임 흐름: 씬 시작 → 준비 → 전투 → (웨이브 클리어) → 준비 → 전투 ...
// GameManager가 생성하며, 하이라키 오브젝트를 필요로 하지 않는다
public class GameStageController
{
    public static GameStageController Instance { get; private set; }

    private StageType currentStage;
    public StageType CurrentStage => currentStage;

    public GameStageController()
    {
        Instance = this;
        EventBus.Instance.Subscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Subscribe<CoreDestroyedEvent>(OnCoreDestroyed);
    }

    // GameManager.Start에서 호출한다
    // 모든 MonoBehaviour의 Awake 완료 후 실행되어야 구독자들이 이벤트를 받을 수 있다
    public void Begin()
    {
        EnterPreparationStage();
    }

    public void Cleanup()
    {
        EventBus.Instance.Unsubscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Unsubscribe<CoreDestroyedEvent>(OnCoreDestroyed);
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

    // 마지막 웨이브 클리어 시에만 준비 단계로 전환한다
    private void OnWaveCleared(WaveClearedEvent e)
    {
        if (!WaveManager.Instance.IsLastWave) return;

        EnterPreparationStage();
    }

    private void OnCoreDestroyed(CoreDestroyedEvent e)
    {
        Debug.Log("게임 오버: 언데드 핵이 파괴되었습니다.");
    }
}
