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

    public TurnType CurrentTurn => currentTurn;
    public int RoundCount => roundCount;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Subscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
        EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Unsubscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
        EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
    }

    // 웨이브(Phase) 시작 시 라운드를 초기화하고 플레이어 턴을 시작한다
    private void OnWaveStarted(WaveStartedEvent e)
    {
        roundCount = 0;
        StartPlayerTurn();
    }

    // 배치 단계 진입 시 턴 상태를 초기화한다
    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (e.CurrentPhase == PhaseType.Preparation)
        {
            isEnemyTurnRunning = false;
            roundCount = 0;
        }
    }

    // 플레이어가 턴 종료 버튼을 눌렀을 때 처리한다
    private void OnEndTurnRequested(EndTurnRequestedEvent e)
    {
        if (currentTurn != TurnType.Player) return;
        if (isEnemyTurnRunning) return;

        StartCoroutine(RunEnemyTurn());
    }

    // 플레이어 턴을 시작한다
    private void StartPlayerTurn()
    {
        currentTurn = TurnType.Player;
        roundCount++;

        foreach (UnitBase unit in UnitRegistry.Instance.GetAliveUndeadUnits())
            unit.ResetTurnState();

        EventBus.Instance.Publish(new TurnChangedEvent { CurrentTurn = TurnType.Player });
    }

    // 적 턴을 그룹 단위로 실행한다
    // 같은 타겟을 목표로 하는 적끼리 그룹으로 묶고 타겟 위치 기준으로 정렬한다
    private IEnumerator RunEnemyTurn()
    {
        isEnemyTurnRunning = true;
        currentTurn = TurnType.Enemy;

        EventBus.Instance.Publish(new TurnChangedEvent { CurrentTurn = TurnType.Enemy });

        IReadOnlyList<UnitBase> allUnits = UnitRegistry.Instance.GetAllUnits();

        // 살아있는 적 유닛의 AI 컴포넌트를 수집한다
        List<EnemyAI> enemyAIs = new List<EnemyAI>();
        foreach (UnitBase unit in allUnits)
        {
            if (unit.Team != TeamType.Enemy || unit.IsDead) continue;

            EnemyAI ai = unit.GetComponent<EnemyAI>();
            if (ai != null)
                enemyAIs.Add(ai);
        }

        // 타겟 위치 기준으로 적을 그룹으로 묶는다
        Dictionary<Vector2Int, List<EnemyAI>> groups = new Dictionary<Vector2Int, List<EnemyAI>>();
        foreach (EnemyAI ai in enemyAIs)
        {
            Vector2Int targetPos = ai.GetTargetGridPosition(new List<UnitBase>(allUnits));
            if (!groups.ContainsKey(targetPos))
                groups[targetPos] = new List<EnemyAI>();
            groups[targetPos].Add(ai);
        }

        // 타겟 x 오름차순, y 내림차순으로 그룹을 정렬한다
        List<Vector2Int> sortedKeys = new List<Vector2Int>(groups.Keys);
        sortedKeys.Sort((a, b) =>
        {
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return b.y.CompareTo(a.y);
        });

        // 그룹 순서대로 각 적의 행동을 실행한다
        foreach (Vector2Int key in sortedKeys)
        {
            foreach (EnemyAI ai in groups[key])
            {
                UnitBase unitBase = ai.GetComponent<UnitBase>();
                if (unitBase == null || unitBase.IsDead) continue;

                yield return StartCoroutine(ai.ExecuteTurn(new List<UnitBase>(allUnits)));
            }

            // 그룹 간 짧은 대기를 두어 가독성을 높인다
            yield return new WaitForSeconds(0.2f);
        }

        isEnemyTurnRunning = false;
        StartPlayerTurn();
    }
}
