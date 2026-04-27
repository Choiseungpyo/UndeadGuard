using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitInfoHUD : MonoBehaviour
{
    [SerializeField] private string bottomLeftPanelName = "BottomLeftUnitPanel";
    [SerializeField] private string unitNameLabelName = "UnitNameLabel";
    [SerializeField] private string unitHpLabelName = "UnitHpLabel";
    [SerializeField] private string unitAtkLabelName = "UnitAtkLabel";
    [SerializeField] private string unitDefLabelName = "UnitDefLabel";
    [SerializeField] private string unitRangeLabelName = "UnitRangeLabel";
    [SerializeField] private string unitMoveLabelName = "UnitMoveLabel";

    private VisualElement panel;
    private Label unitNameLabel;
    private Label unitHpLabel;
    private Label unitAtkLabel;
    private Label unitDefLabel;
    private Label unitRangeLabel;
    private Label unitMoveLabel;

    private UnitBase selectedUnit;

    private void OnEnable()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        panel = root.Q<VisualElement>(bottomLeftPanelName);
        unitNameLabel = root.Q<Label>(unitNameLabelName);
        unitHpLabel = root.Q<Label>(unitHpLabelName);
        unitAtkLabel = root.Q<Label>(unitAtkLabelName);
        unitDefLabel = root.Q<Label>(unitDefLabelName);
        unitRangeLabel = root.Q<Label>(unitRangeLabelName);
        unitMoveLabel = root.Q<Label>(unitMoveLabelName);

        Hide();

        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<UnitAttackedEvent>(OnUnitAttacked);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<UnitAttackedEvent>(OnUnitAttacked);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnUnitSelected(UnitSelectedEvent e)
    {
        if (e.Unit.Team != TeamType.Undead)
            return;

        selectedUnit = e.Unit;
        Refresh(e.Unit);
        Show();
    }

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        Hide();
    }

    // 선택된 유닛이 피격되면 HP 라벨만 갱신한다.
    private void OnUnitAttacked(UnitAttackedEvent e)
    {
        if (selectedUnit == null || e.Target != selectedUnit)
            return;

        RefreshHp(selectedUnit);
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn != TurnType.Enemy)
            return;

        selectedUnit = null;
        Hide();
    }

    private void Refresh(UnitBase unit)
    {
        if (unitNameLabel != null) unitNameLabel.text = unit.gameObject.name;
        if (unitAtkLabel != null) unitAtkLabel.text = $"ATK : {unit.Stats.PhysicalAttack}";
        if (unitDefLabel != null) unitDefLabel.text = $"DEF : {unit.Stats.DefensePower}";
        if (unitRangeLabel != null) unitRangeLabel.text = $"사거리 : {GetDisplayAttackRange(unit)}";
        if (unitMoveLabel != null) unitMoveLabel.text = $"이동 : {unit.Stats.MoveRange}";
        RefreshHp(unit);
    }

    private static int GetDisplayAttackRange(UnitBase unit)
    {
        if (unit == null)
            return 0;

        int fallbackRange = Mathf.Max(0, unit.Stats.AttackRange);
        List<Vector2Int> offsets = AttackPatternResolver.GetRelativeTargetOffsets(
            unit,
            AttackActionIds.BasicAttack,
            fallbackRange);

        if (offsets == null || offsets.Count == 0)
            return fallbackRange;

        int maxDistance = 0;
        for (int i = 0; i < offsets.Count; i++)
        {
            Vector2Int offset = offsets[i];
            int distance = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            if (distance > maxDistance)
                maxDistance = distance;
        }

        return Mathf.Max(maxDistance, fallbackRange);
    }

    private void RefreshHp(UnitBase unit)
    {
        if (unitHpLabel != null)
            unitHpLabel.text = $"HP : {unit.Stats.CurrentHp} / {unit.Stats.MaxHp}";
    }

    private void Show()
    {
        if (panel != null)
            panel.style.display = DisplayStyle.Flex;
    }

    private void Hide()
    {
        if (panel != null)
            panel.style.display = DisplayStyle.None;
    }
}
