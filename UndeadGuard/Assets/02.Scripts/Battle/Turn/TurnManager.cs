using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 전투 단계에서 플레이어 턴과 적 턴을 관리한다
// WaveStartedEvent를 받아 플레이어 턴을 시작하고 EndTurnRequestedEvent로 턴을 종료한다
public class TurnManager : MonoBehaviour
{
    // 현재 진행 중인 턴 주체
    private TurnType currentTurn;

    // 적 턴이 진행 중인지 여부 (중복 실행 방지)
    private bool isEnemyTurnRunning;

    // 현재 웨이브 내 라운드 수 (웨이브 시작마다 초기화)
    private int roundCount;

    [Header("Enemy Turn Timing")]
    [SerializeField] private float moveToAttackDelay = 0.3f;
    [SerializeField] private float attackStepDelay = 0.45f;
    [SerializeField] private float endOfEnemyTurnDelay = 0.9f;

    public TurnType CurrentTurn => currentTurn;
    public int RoundCount => roundCount;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Subscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
        EventBus.Instance.Subscribe<StageChangedEvent>(OnStageChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Unsubscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
        EventBus.Instance.Unsubscribe<StageChangedEvent>(OnStageChanged);
    }

    // 웨이브(Phase) 시작 시 라운드를 초기화하고 플레이어 턴을 시작한다
    private void OnWaveStarted(WaveStartedEvent e)
    {
        roundCount = 0;
        StartPlayerTurn();
    }

    // 배치 단계 진입 시 턴 상태를 초기화한다
    private void OnStageChanged(StageChangedEvent e)
    {
        if (e.CurrentStage == StageType.Preparation)
        {
            isEnemyTurnRunning = false;
            roundCount = 0;
        }
    }

    #region Player Turn

    // 플레이어 턴을 시작한다
    private void StartPlayerTurn()
    {
        currentTurn = TurnType.Player;
        roundCount++;

        foreach (UnitBase unit in UnitRegistry.Instance.GetAliveUndeadUnits())
            unit.ResetTurnState();

        EventBus.Instance.Publish(new TurnChangedEvent { CurrentTurn = TurnType.Player });
    }

    // 플레이어가 턴 종료 버튼을 눌렀을 때 처리한다
    private void OnEndTurnRequested(EndTurnRequestedEvent e)
    {
        if (currentTurn != TurnType.Player) return;
        if (isEnemyTurnRunning) return;

        StartCoroutine(RunEnemyTurn());
    }

    #endregion

    #region Enemy Turn

    // 적 턴을 이동 단계 → 공격 단계 순으로 실행한다
    // 공격/피격 연출이 끝날 시간을 확보한 뒤에 플레이어 턴으로 전환한다.
    private IEnumerator RunEnemyTurn()
    {
        isEnemyTurnRunning = true;
        currentTurn = TurnType.Enemy;

        EventBus.Instance.Publish(new TurnChangedEvent { CurrentTurn = TurnType.Enemy });

        // 턴 시작 시점의 유닛 목록을 스냅샷으로 고정한다
        List<UnitBase> allUnits = new List<UnitBase>(UnitRegistry.Instance.GetAllUnits());

        List<EnemyAI> enemyAIs = new List<EnemyAI>();
        foreach (UnitBase unit in allUnits)
        {
            if (unit.Team != TeamType.Enemy || unit.IsDead) continue;

            EnemyAI ai = unit.GetComponent<EnemyAI>();
            if (ai != null)
                enemyAIs.Add(ai);
            else
                Debug.LogWarning($"[TurnManager] 적 유닛 {unit.name}에 EnemyAI 컴포넌트가 없습니다.");
        }

        if (enemyAIs.Count == 0)
            Debug.LogWarning("[TurnManager] 행동할 적 유닛이 없습니다. UnitRegistry 등록 여부와 EnemyAI 컴포넌트를 확인하세요.");

        // 1단계: 모든 적이 순서대로 이동한다
        foreach (EnemyAI ai in enemyAIs)
        {
            if (ai.GetComponent<UnitBase>().IsDead) continue;
            yield return StartCoroutine(ai.ExecuteMove(allUnits));
        }

        float moveDelay = Mathf.Max(0f, moveToAttackDelay);
        if (moveDelay > 0f)
            yield return new WaitForSeconds(moveDelay);

        // 2단계: 모든 적이 순서대로 공격한다
        float perAttackDelay = Mathf.Max(0f, attackStepDelay);
        foreach (EnemyAI ai in enemyAIs)
        {
            if (ai.GetComponent<UnitBase>().IsDead) continue;

            bool attacked = ai.ExecuteAttack(allUnits);
            if (attacked && perAttackDelay > 0f)
                yield return new WaitForSeconds(perAttackDelay);
        }

        float endDelay = Mathf.Max(0f, endOfEnemyTurnDelay);
        if (endDelay > 0f)
            yield return new WaitForSeconds(endDelay);

        isEnemyTurnRunning = false;
        StartPlayerTurn();
    }

    #endregion
}
