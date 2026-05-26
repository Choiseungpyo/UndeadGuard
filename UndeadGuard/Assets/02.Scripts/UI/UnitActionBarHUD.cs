using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitActionBarHUD : MonoBehaviour
{
    [SerializeField] private string actionBarName = "BottomCenterActionBar";
    [SerializeField] private string actionButton0Name = "ActionButton0";
    [SerializeField] private string actionButton1Name = "ActionButton1";

    private VisualElement actionBar;
    private Button firstActionButton;
    private Button secondActionButton;
    private UIDocument uiDocument;

    private UndeadUnit selectedUnit;
    private bool isUnitMoving;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found.");
            return;
        }

        BattleInputGuard.Instance.RegisterDocument(uiDocument);

        VisualElement root = uiDocument.rootVisualElement;

        actionBar = root.Q<VisualElement>(actionBarName);
        firstActionButton = root.Q<Button>(actionButton0Name);
        secondActionButton = root.Q<Button>(actionButton1Name);

        if (firstActionButton != null)
            firstActionButton.RegisterCallback<PointerDownEvent>(OnFirstActionPointerDown, TrickleDown.TrickleDown);

        if (secondActionButton != null)
            secondActionButton.RegisterCallback<PointerDownEvent>(OnSecondActionPointerDown, TrickleDown.TrickleDown);

        Hide();

        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Subscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Subscribe<TutorialStepChangedEvent>(OnTutorialStepChanged);
    }

    private void OnDisable()
    {
        if (firstActionButton != null)
            firstActionButton.UnregisterCallback<PointerDownEvent>(OnFirstActionPointerDown, TrickleDown.TrickleDown);

        if (secondActionButton != null)
            secondActionButton.UnregisterCallback<PointerDownEvent>(OnSecondActionPointerDown, TrickleDown.TrickleDown);

        if (uiDocument != null)
        {
            if (BattleInputGuard.TryGetExisting(out var guard))
                guard.UnregisterDocument(uiDocument);
        }

        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Unsubscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Unsubscribe<TutorialStepChangedEvent>(OnTutorialStepChanged);
    }

    private void OnUnitSelected(UnitSelectedEvent e)
    {
        if (e.Unit.Team != TeamType.Undead)
            return;

        selectedUnit = e.Unit as UndeadUnit;
        if (selectedUnit == null)
            return;

        RefreshActionButtons();
        Show();
    }

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        Hide();
    }

    private void OnUnitMoveStarted(UnitMoveStartedEvent e)
    {
        isUnitMoving = true;
        RefreshActionButtons();
    }

    private void OnUnitMoveFinished(UnitMoveFinishedEvent e)
    {
        isUnitMoving = false;
        RefreshActionButtons();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn != TurnType.Enemy)
            return;

        selectedUnit = null;
        Hide();
    }

    private void OnTutorialStepChanged(TutorialStepChangedEvent e)
    {
        RefreshActionButtons();
    }

    private void OnFirstActionPointerDown(PointerDownEvent e)
    {
        RequestActionAtIndex(0);
    }

    private void OnSecondActionPointerDown(PointerDownEvent e)
    {
        RequestActionAtIndex(1);
    }

    private void RequestActionAtIndex(int actionIndex)
    {
        IUnitAction action = GetActionAtIndex(actionIndex);
        if (isUnitMoving)
            return;

        if (!CanUseAction(action))
            return;

        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanPressAttackButton())
            return;

        UIFieldInputBlocker.RequestSkipNextWorldClick();
        EventBus.Instance.Publish(new ActionModeRequestedEvent
        {
            Unit = selectedUnit,
            Action = action
        });

        EventBus.Instance.Publish(new AttackModeRequestedEvent
        {
            Unit = selectedUnit,
            Action = action
        });
    }

    private void RefreshActionButtons()
    {
        RefreshActionButton(firstActionButton, 0);
        RefreshActionButton(secondActionButton, 1);
    }

    private void RefreshActionButton(Button button, int actionIndex)
    {
        if (button == null)
            return;

        IUnitAction action = GetActionAtIndex(actionIndex);
        bool canUse = !isUnitMoving && CanUseAction(action);
        if (canUse && TutorialManager.Instance != null)
            canUse = TutorialManager.Instance.CanPressAttackButton();

        button.style.display = action != null ? DisplayStyle.Flex : DisplayStyle.None;
        button.SetEnabled(canUse);
        button.text = action != null && !string.IsNullOrWhiteSpace(action.DisplayName)
            ? action.DisplayName
            : "Action";
    }

    private IUnitAction GetActionAtIndex(int actionIndex)
    {
        if (selectedUnit == null)
            return null;

        IReadOnlyList<IUnitAction> actions = selectedUnit.GetActions();
        if (actionIndex < 0 || actionIndex >= actions.Count)
            return null;

        return actions[actionIndex];
    }

    private static bool CanUseAction(IUnitAction action)
    {
        return action != null && action.CanUse();
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
