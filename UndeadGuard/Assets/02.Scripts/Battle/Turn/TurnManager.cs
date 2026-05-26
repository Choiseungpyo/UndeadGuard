using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 전투 단계에서 플레이어 턴과 적 턴을 관리한다
// WaveStartedEvent를 받아 플레이어 턴을 시작하고 EndTurnRequestedEvent로 턴을 종료한다
public class TurnManager : MonoBehaviour
{
    [SerializeField] private ActionCameraDirector actionCameraDirector;

    // 현재 진행 중인 턴 주체
    private TurnType currentTurn;

    // 적 턴이 진행 중인지 여부 (중복 실행 방지)
    private bool isEnemyTurnRunning;
    private int activePlayerUndeadMoveCount;
    private bool battleEnded;
    private bool waveClearedDuringEnemyTurn;

    // 현재 웨이브 내 라운드 수 (웨이브 시작마다 초기화)
    private int roundCount;
    private BattleInputGuard inputGuard;

    [Header("Enemy Turn Timing")]
    [SerializeField] private float moveToAttackDelay = 0.3f;
    [SerializeField] private float attackStepDelay = 0.45f;
    [SerializeField] private float enemyGroupAttackDelay = 0.4f;
    [SerializeField] private float endOfEnemyTurnDelay = 0.9f;

    public TurnType CurrentTurn => currentTurn;
    public int RoundCount => roundCount;

    private void Awake()
    {
        ResolveActionCameraDirectorIfNeeded();
        ResolveInputGuardIfNeeded();
    }

    private void OnEnable()
    {
        ResolveActionCameraDirectorIfNeeded();
        ResolveInputGuardIfNeeded();

        EventBus.Instance.Subscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Subscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
        EventBus.Instance.Subscribe<StageChangedEvent>(OnStageChanged);
        EventBus.Instance.Subscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Subscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Subscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Subscribe<BattleWonEvent>(OnBattleWon);
        EventBus.Instance.Subscribe<CoreDestroyedEvent>(OnCoreDestroyed);
    }

    private void OnDisable()
    {
        SetActionCameraLock(false);

        EventBus.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Unsubscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
        EventBus.Instance.Unsubscribe<StageChangedEvent>(OnStageChanged);
        EventBus.Instance.Unsubscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Unsubscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Unsubscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Unsubscribe<BattleWonEvent>(OnBattleWon);
        EventBus.Instance.Unsubscribe<CoreDestroyedEvent>(OnCoreDestroyed);
    }

    // 웨이브(Phase) 시작 시 라운드를 초기화하고 플레이어 턴을 시작한다
    private void OnWaveStarted(WaveStartedEvent e)
    {
        battleEnded = false;
        roundCount = 0;
        activePlayerUndeadMoveCount = 0;
        StartPlayerTurn();
    }

    // 배치 단계 진입 시 턴 상태를 초기화한다
    private void OnStageChanged(StageChangedEvent e)
    {
        if (e.CurrentStage == StageType.Preparation)
        {
            battleEnded = false;
            isEnemyTurnRunning = false;
            waveClearedDuringEnemyTurn = false;
            roundCount = 0;
            activePlayerUndeadMoveCount = 0;
        }
    }

    private void OnBattleWon(BattleWonEvent e)
    {
        battleEnded = true;
        isEnemyTurnRunning = false;
        activePlayerUndeadMoveCount = 0;
    }

    private void OnCoreDestroyed(CoreDestroyedEvent e)
    {
        battleEnded = true;
        isEnemyTurnRunning = false;
        activePlayerUndeadMoveCount = 0;
    }

    private void OnUnitMoveStarted(UnitMoveStartedEvent e)
    {
        if (currentTurn != TurnType.Player)
            return;

        if (e.Unit == null || e.Unit.Team != TeamType.Undead)
            return;

        activePlayerUndeadMoveCount++;
    }

    private void OnUnitMoveFinished(UnitMoveFinishedEvent e)
    {
        if (currentTurn != TurnType.Player)
            return;

        if (e.Unit == null || e.Unit.Team != TeamType.Undead)
            return;

        activePlayerUndeadMoveCount = Mathf.Max(0, activePlayerUndeadMoveCount - 1);
    }

    private void OnWaveCleared(WaveClearedEvent e)
    {
        if (currentTurn == TurnType.Enemy)
            waveClearedDuringEnemyTurn = true;
    }

    #region Player Turn

    // 플레이어 턴을 시작한다
    private void StartPlayerTurn()
    {
        if (battleEnded)
            return;

        currentTurn = TurnType.Player;
        roundCount++;
        activePlayerUndeadMoveCount = 0;

        foreach (UnitBase unit in UnitRegistry.Instance.GetAliveUndeadUnits())
            unit.ResetTurnState();

        EventBus.Instance.Publish(new TurnChangedEvent { CurrentTurn = TurnType.Player });
        EventBus.Instance.Publish(new PlayerTurnStartedEvent());
    }

    // 플레이어가 턴 종료 버튼을 눌렀을 때 처리한다
    private void OnEndTurnRequested(EndTurnRequestedEvent e)
    {
        if (battleEnded) return;
        if (currentTurn != TurnType.Player) return;
        if (isEnemyTurnRunning) return;
        if (activePlayerUndeadMoveCount > 0) return;

        StartCoroutine(RunEnemyTurn());
    }

    #endregion

    #region Enemy Turn

    // 적 턴을 이동 단계 → 공격 단계 순으로 실행한다
    // 공격/피격 연출이 끝날 시간을 확보한 뒤에 플레이어 턴으로 전환한다.
    private IEnumerator RunEnemyTurn()
    {
        if (battleEnded)
            yield break;

        isEnemyTurnRunning = true;
        waveClearedDuringEnemyTurn = false;
        currentTurn = TurnType.Enemy;
        activePlayerUndeadMoveCount = 0;

        EventBus.Instance.Publish(new TurnEndedEvent
        {
            EndedTurn = TurnType.Player,
            NextTurn = TurnType.Enemy
        });

        EventBus.Instance.Publish(new TurnChangedEvent { CurrentTurn = TurnType.Enemy });

        SyncUndeadGridPositionsFromWorld();

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
        List<EnemyMovePlan> movePlans = new List<EnemyMovePlan>();
        HashSet<Vector2Int> reservedDestinations = new HashSet<Vector2Int>();
        foreach (EnemyAI ai in enemyAIs)
        {
            UnitBase enemyUnit = ai.GetComponent<UnitBase>();
            if (enemyUnit == null || enemyUnit.IsDead)
                continue;

            if (!ai.TryCreateMovePlan(allUnits, reservedDestinations, out EnemyMovePlan movePlan))
                continue;

            movePlans.Add(movePlan);
            reservedDestinations.Add(movePlan.Destination);
        }

        List<List<EnemyMovePlan>> groupedMovePlans = GroupMovePlansByTarget(movePlans);
        for (int groupIndex = 0; groupIndex < groupedMovePlans.Count; groupIndex++)
        {
            List<EnemyMovePlan> moveGroup = groupedMovePlans[groupIndex];
            yield return StartCoroutine(ExecuteEnemyMoveGroup(moveGroup));
        }

        float moveDelay = Mathf.Max(0f, moveToAttackDelay);
        if (moveDelay > 0f)
            yield return new WaitForSeconds(moveDelay);

        // 2단계: 모든 적이 순서대로 공격한다
        List<EnemyAttackPlan> attackPlans = new List<EnemyAttackPlan>();
        foreach (EnemyAI ai in enemyAIs)
        {
            if (ai.GetComponent<UnitBase>().IsDead)
                continue;

            if (ai.TryCreateAttackPlan(allUnits, out EnemyAttackPlan plan))
                attackPlans.Add(plan);
            else
                ai.HandleNoAttackSideEffects();
        }

        List<List<EnemyAttackPlan>> groupedPlans = GroupAttackPlansByTarget(attackPlans);
        float perAttackDelay = Mathf.Max(0f, attackStepDelay);
        for (int groupIndex = 0; groupIndex < groupedPlans.Count; groupIndex++)
        {
            List<EnemyAttackPlan> group = groupedPlans[groupIndex];
            yield return StartCoroutine(ExecuteEnemyAttackGroup(group));

            // Keep sequential pacing between different target groups.
            if (perAttackDelay > 0f && groupIndex < groupedPlans.Count - 1)
                yield return new WaitForSeconds(perAttackDelay);
        }

        float endDelay = Mathf.Max(0f, endOfEnemyTurnDelay);
        if (endDelay > 0f)
            yield return new WaitForSeconds(endDelay);

        isEnemyTurnRunning = false;
        EventBus.Instance.Publish(new EnemyTurnFinishedEvent());

        if (!battleEnded && !waveClearedDuringEnemyTurn)
            StartPlayerTurn();
    }

    private static void SyncUndeadGridPositionsFromWorld()
    {
        if (GridManager.Instance == null || UnitRegistry.Instance == null)
            return;

        IReadOnlyList<UnitBase> units = UnitRegistry.Instance.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
        {
            UnitBase unit = units[i];
            if (unit == null || unit.IsDead || unit.Team != TeamType.Undead)
                continue;

            Vector2Int currentGrid = GridManager.Instance.WorldToGrid(unit.transform.position);
            unit.SetGridPosition(currentGrid);
        }
    }

    private static List<List<EnemyAttackPlan>> GroupAttackPlansByTarget(IReadOnlyList<EnemyAttackPlan> plans)
    {
        List<List<EnemyAttackPlan>> groups = new List<List<EnemyAttackPlan>>();
        for (int i = 0; i < plans.Count; i++)
        {
            EnemyAttackPlan plan = plans[i];
            if (plan == null)
                continue;

            if (plan.Target == null)
            {
                groups.Add(new List<EnemyAttackPlan> { plan });
                continue;
            }

            bool added = false;
            for (int g = 0; g < groups.Count; g++)
            {
                List<EnemyAttackPlan> existingGroup = groups[g];
                if (existingGroup.Count == 0)
                    continue;

                EnemyAttackPlan first = existingGroup[0];
                if (first != null && object.ReferenceEquals(first.Target, plan.Target))
                {
                    existingGroup.Add(plan);
                    added = true;
                    break;
                }
            }

            if (!added)
                groups.Add(new List<EnemyAttackPlan> { plan });
        }

        return groups;
    }

    private static List<List<EnemyMovePlan>> GroupMovePlansByTarget(IReadOnlyList<EnemyMovePlan> plans)
    {
        List<List<EnemyMovePlan>> groups = new List<List<EnemyMovePlan>>();
        for (int i = 0; i < plans.Count; i++)
        {
            EnemyMovePlan plan = plans[i];
            if (plan == null)
                continue;

            if (plan.TargetKey == null)
            {
                groups.Add(new List<EnemyMovePlan> { plan });
                continue;
            }

            bool added = false;
            for (int g = 0; g < groups.Count; g++)
            {
                List<EnemyMovePlan> existingGroup = groups[g];
                if (existingGroup.Count == 0)
                    continue;

                EnemyMovePlan first = existingGroup[0];
                if (first != null && object.ReferenceEquals(first.TargetKey, plan.TargetKey))
                {
                    existingGroup.Add(plan);
                    added = true;
                    break;
                }
            }

            if (!added)
                groups.Add(new List<EnemyMovePlan> { plan });
        }

        return groups;
    }

    private IEnumerator ExecuteEnemyMoveGroup(List<EnemyMovePlan> group)
    {
        if (group == null || group.Count == 0)
            yield break;

        int remaining = 0;
        for (int i = 0; i < group.Count; i++)
        {
            EnemyMovePlan plan = group[i];
            if (plan == null || plan.EnemyAI == null)
                continue;

            remaining++;
            StartCoroutine(RunEnemyMovePlan(plan, () => remaining--));
        }

        if (remaining <= 0)
            yield break;

        yield return new WaitUntil(() => remaining <= 0);
    }

    private IEnumerator ExecuteEnemyAttackGroup(List<EnemyAttackPlan> group)
    {
        if (group == null || group.Count == 0)
            yield break;

        ResolveActionCameraDirectorIfNeeded();
        ResolveInputGuardIfNeeded();

        List<Transform> attackerTransforms = new List<Transform>();
        Transform targetTransform = null;
        for (int i = 0; i < group.Count; i++)
        {
            EnemyAttackPlan plan = group[i];
            if (plan == null)
                continue;

            if (plan.AttackerUnit != null)
                attackerTransforms.Add(plan.AttackerUnit.transform);

            if (targetTransform == null && plan.TargetTransform != null)
                targetTransform = plan.TargetTransform;
        }

        bool canUseGroupCamera = actionCameraDirector != null
            && targetTransform != null
            && attackerTransforms.Count > 0;

        if (canUseGroupCamera)
            SetActionCameraLock(true);

        try
        {
            if (canUseGroupCamera)
                yield return RunCameraRoutineSafely(
                    actionCameraDirector.PlayEnemyGroupAttackCamera(attackerTransforms, targetTransform),
                    "PlayEnemyGroupAttackCamera");

            for (int i = 0; i < group.Count; i++)
            {
                EnemyAttackPlan plan = group[i];
                if (plan == null || plan.EnemyAI == null)
                    continue;

                plan.EnemyAI.ExecuteAttackPlan(plan);
            }

            float groupDelay = Mathf.Max(0f, enemyGroupAttackDelay);
            if (groupDelay > 0f)
                yield return new WaitForSeconds(groupDelay);

            if (canUseGroupCamera)
            {
                yield return RunCameraRoutineSafely(
                    actionCameraDirector.HoldBeforeReturn(),
                    "HoldBeforeReturn");

                yield return RunCameraRoutineSafely(
                    actionCameraDirector.ReturnToSavedCamera(),
                    "ReturnToSavedCamera");
            }
        }
        finally
        {
            if (canUseGroupCamera)
                SetActionCameraLock(false);

            EventBus.Instance.Publish(new ActionCameraFlowFinishedEvent());
        }
    }

    private void ResolveActionCameraDirectorIfNeeded()
    {
        if (actionCameraDirector != null)
            return;

        actionCameraDirector = ActionCameraDirector.Instance;
    }

    private void ResolveInputGuardIfNeeded()
    {
        if (inputGuard != null)
            return;

        inputGuard = BattleInputGuard.Instance;
    }

    private void SetActionCameraLock(bool active)
    {
        ResolveInputGuardIfNeeded();
        if (inputGuard != null)
            inputGuard.SetActionCameraActive(active);
    }

    private IEnumerator RunEnemyMovePlan(EnemyMovePlan plan, System.Action onCompleted)
    {
        try
        {
            if (plan == null || plan.EnemyAI == null)
                yield break;

            IEnumerator routine = plan.EnemyAI.ExecuteMovePlan(plan);
            if (routine == null)
                yield break;

            while (true)
            {
                object current;
                bool movedNext;
                try
                {
                    movedNext = routine.MoveNext();
                    if (!movedNext)
                        yield break;

                    current = routine.Current;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[TurnManager] ExecuteMovePlan failed: {ex.Message}");
                    yield break;
                }

                yield return current;
            }
        }
        finally
        {
            onCompleted?.Invoke();
        }
    }

    private IEnumerator RunCameraRoutineSafely(IEnumerator routine, string routineName)
    {
        if (routine == null)
            yield break;

        while (true)
        {
            object current;
            bool movedNext;
            try
            {
                movedNext = routine.MoveNext();
                if (!movedNext)
                    yield break;

                current = routine.Current;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[TurnManager] {routineName} failed: {ex.Message}");
                yield break;
            }

            yield return current;
        }
    }

    #endregion
}
