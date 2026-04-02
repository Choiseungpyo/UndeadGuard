using System.Collections.Generic;
using UnityEngine;

// 플레이어 유닛의 기본 공격 처리를 담당한다
// 유닛 선택 시 공격 가능한 적 위치를 표시하고 AttackRequestedEvent를 받아 공격을 실행한다
public class ActionHandler : MonoBehaviour
{
    // 현재 선택된 언데드 유닛
    private UnitBase selectedUnit;

    // 현재 공격 가능한 적 유닛 목록
    private List<UnitBase> attackableTargets = new List<UnitBase>();

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Subscribe<AttackRequestedEvent>(OnAttackRequested);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Unsubscribe<AttackRequestedEvent>(OnAttackRequested);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
    }

    // 유닛이 선택되면 공격 가능 범위를 표시한다
    // 전투 단계가 아닌 경우 공격 범위를 표시하지 않는다
    private void OnUnitSelected(UnitSelectedEvent e)
    {
        if (GamePhaseController.Instance.CurrentPhase != PhaseType.Battle) return;

        selectedUnit = e.Unit;
        RefreshAttackHighlight();
    }

    // 유닛 선택이 해제되면 공격 범위 하이라이트를 제거한다
    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        attackableTargets.Clear();
        GridHighlighter.Instance.ClearAttackable();
    }

    // 유닛 이동 완료 후 현재 위치에서 공격 가능 범위를 다시 계산한다
    private void OnUnitMoveFinished(UnitMoveFinishedEvent e)
    {
        if (selectedUnit != e.Unit) return;
        RefreshAttackHighlight();
    }

    // 적 턴으로 전환되면 공격 범위 하이라이트를 제거한다
    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn == TurnType.Enemy)
        {
            selectedUnit = null;
            attackableTargets.Clear();
            GridHighlighter.Instance.ClearAttackable();
        }
    }

    // 플레이어가 적 유닛을 클릭하면 기본 공격을 수행한다
    private void OnAttackRequested(AttackRequestedEvent e)
    {
        if (selectedUnit == null) return;
        if (selectedUnit.HasActed) return;
        if (!attackableTargets.Contains(e.Target)) return;

        // 기본 공격 실행
        selectedUnit.UnitAnimator?.TriggerAttack();
        e.Target.TakeDamage(selectedUnit.Stats.PhysicalAttack);

        EventBus.Instance.Publish(new UnitAttackedEvent
        {
            Attacker = selectedUnit,
            Target = e.Target,
            Damage = selectedUnit.Stats.PhysicalAttack
        });

        selectedUnit.MarkAsActed();
        GridHighlighter.Instance.ClearAttackable();
        attackableTargets.Clear();

        // 이동까지 완료되었으면 선택을 해제한다
        if (selectedUnit.HasMoved)
        {
            EventBus.Instance.Publish(new UnitDeselectedEvent());
        }
    }

    // 현재 선택된 유닛의 위치에서 공격 가능한 적 목록을 계산하고 하이라이트를 표시한다
    private void RefreshAttackHighlight()
    {
        attackableTargets.Clear();
        GridHighlighter.Instance.ClearAttackable();

        if (selectedUnit == null || selectedUnit.HasActed) return;

        List<Vector2Int> positions = new List<Vector2Int>();

        UnitBase[] allUnits = FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        foreach (UnitBase u in allUnits)
        {
            if (u.Team != TeamType.Enemy || u.IsDead) continue;

            int dist = Manhattan(selectedUnit.GridPosition, u.GridPosition);
            if (dist <= selectedUnit.Stats.AttackRange)
            {
                attackableTargets.Add(u);
                positions.Add(u.GridPosition);
            }
        }

        GridHighlighter.Instance.ShowAttackable(positions);
    }

    private int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
