using UnityEngine;
using UnityEngine.InputSystem;

// 전투 단계에서 플레이어의 마우스 입력을 처리한다
// 플레이어 턴일 때만 입력을 받으며, 적 턴 중에는 입력을 차단한다
public class BattleInputController : MonoBehaviour
{
    // 전투 페이즈 활성화 여부. 준비 단계에서는 입력을 차단한다
    private bool isBattlePhaseActive;

    // 입력 처리 활성화 여부. 적 턴 중에는 false로 설정된다
    private bool isInputEnabled;

    // 현재 선택된 언데드 유닛
    private UnitBase selectedUnit;

    // 유닛 이동 중 입력을 차단하기 위한 플래그
    private bool isUnitMoving;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Subscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Unsubscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
    }

    // 페이즈 전환 시 전투 단계가 아니면 입력을 차단하고 선택을 해제한다
    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        isBattlePhaseActive = (e.CurrentPhase == PhaseType.Battle);

        if (!isBattlePhaseActive)
        {
            isInputEnabled = false;
            DeselectUnit();
        }
    }

    private void OnUnitMoveStarted(UnitMoveStartedEvent e)
    {
        isUnitMoving = true;
    }

    private void OnUnitMoveFinished(UnitMoveFinishedEvent e)
    {
        isUnitMoving = false;
    }

    // 이벤트로 선택 해제가 발생했을 때 내부 상태도 갱신한다
    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
    }

    // 턴 변경 이벤트를 받아 입력 활성화 여부를 갱신한다
    // 전투 페이즈가 아닌 경우 무시한다
    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (!isBattlePhaseActive) return;

        isInputEnabled = e.CurrentTurn == TurnType.Player;

        // 적 턴으로 전환되면 선택 상태를 해제한다
        if (!isInputEnabled)
            DeselectUnit();
    }

    private void Update()
    {
        if (!isInputEnabled) return;
        if (isUnitMoving) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleLeftClick();
    }

    // 마우스 왼쪽 클릭을 처리한다
    private void HandleLeftClick()
    {
        var mousePos = Mouse.current.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out var hit)) return;

        var unit = hit.collider.GetComponentInParent<UnitBase>();

        // 선택된 유닛이 있고 클릭한 대상이 살아있는 적 유닛이면 공격 요청을 발행한다
        if (unit != null && unit.Team == TeamType.Enemy && !unit.IsDead && selectedUnit != null)
        {
            EventBus.Instance.Publish(new AttackRequestedEvent { Target = unit });
            return;
        }

        // 선택 없이 적 유닛을 클릭하면 아무 동작도 하지 않는다
        if (unit != null && unit.Team == TeamType.Enemy)
            return;

        // 클릭한 대상이 살아있는 언데드 유닛이면 선택한다
        if (unit != null && unit.Team == TeamType.Undead && !unit.IsDead)
        {
            SelectUnit(unit);
            return;
        }

        // 유닛이 선택된 상태에서 빈 타일을 클릭하면 이동 시도 이벤트를 발행한다
        if (selectedUnit != null)
        {
            var gridPos = GridManager.Instance.WorldToGrid(hit.point);
            EventBus.Instance.Publish(new TileClickedEvent { GridPosition = gridPos });
            return;
        }
    }

    // 유닛을 선택하고 UnitSelectedEvent를 발행한다
    private void SelectUnit(UnitBase unit)
    {
        selectedUnit = unit;

        EventBus.Instance.Publish(new UnitSelectedEvent
        {
            Unit = unit
        });
    }

    // 선택된 유닛을 해제하고 UnitDeselectedEvent를 발행한다
    private void DeselectUnit()
    {
        if (selectedUnit == null) return;

        selectedUnit = null;
        EventBus.Instance.Publish(new UnitDeselectedEvent());
    }

    // 현재 선택된 유닛을 반환한다
    public UnitBase GetSelectedUnit() => selectedUnit;
}
