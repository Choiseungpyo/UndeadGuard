using UnityEngine;
using UnityEngine.UIElements;

public class UnitActionBarHUD : MonoBehaviour
{
    [SerializeField] private string actionBarName = "BottomCenterActionBar";
    [SerializeField] private string attackButtonName = "AttackButton";
    [SerializeField] private string skillButtonName = "SkillButton";

    private VisualElement actionBar;
    private Button attackButton;
    private Button skillButton;

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

        actionBar = root.Q<VisualElement>(actionBarName);
        attackButton = root.Q<Button>(attackButtonName);
        skillButton = root.Q<Button>(skillButtonName);

        if (attackButton != null)
            attackButton.RegisterCallback<PointerDownEvent>(OnAttackPointerDown, TrickleDown.TrickleDown);

        if (skillButton != null)
            skillButton.RegisterCallback<PointerDownEvent>(OnSkillPointerDown, TrickleDown.TrickleDown);

        Hide();

        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnDisable()
    {
        if (attackButton != null)
            attackButton.UnregisterCallback<PointerDownEvent>(OnAttackPointerDown, TrickleDown.TrickleDown);

        if (skillButton != null)
            skillButton.UnregisterCallback<PointerDownEvent>(OnSkillPointerDown, TrickleDown.TrickleDown);

        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnUnitSelected(UnitSelectedEvent e)
    {
        if (e.Unit.Team != TeamType.Undead)
            return;

        selectedUnit = e.Unit;
        RefreshSkillButtonState();
        Show();
    }

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        Hide();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn != TurnType.Enemy)
            return;

        selectedUnit = null;
        Hide();
    }

    private void OnAttackPointerDown(PointerDownEvent e)
    {
        UIWorldInputGuard.RequestSkipNextWorldClick();
        EventBus.Instance.Publish(new AttackModeRequestedEvent());
    }

    private void OnSkillPointerDown(PointerDownEvent e)
    {
        if (!CanUseSkill(selectedUnit))
            return;

        UIWorldInputGuard.RequestSkipNextWorldClick();
        EventBus.Instance.Publish(new SkillModeRequestedEvent());
    }

    private void RefreshSkillButtonState()
    {
        if (skillButton == null)
            return;

        bool canUse = CanUseSkill(selectedUnit);
        skillButton.style.display = canUse ? DisplayStyle.Flex : DisplayStyle.None;
        skillButton.SetEnabled(canUse);

        UndeadUnit undead = selectedUnit as UndeadUnit;
        ISkill skill = undead != null ? undead.GetSkill() : null;
        if (skill != null && !string.IsNullOrWhiteSpace(skill.SkillName))
            skillButton.text = skill.SkillName;
        else
            skillButton.text = "Skill";
    }

    private static bool CanUseSkill(UnitBase unit)
    {
        UndeadUnit undead = unit as UndeadUnit;
        return undead != null && undead.GetSkill() != null;
    }

    private void Show()
    {
        if (actionBar != null)
            actionBar.style.display = DisplayStyle.Flex;
    }

    private void Hide()
    {
        if (actionBar != null)
            actionBar.style.display = DisplayStyle.None;
    }
}