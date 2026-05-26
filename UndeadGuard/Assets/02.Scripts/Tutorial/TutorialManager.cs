using UnityEngine;
using UnityEngine.Serialization;

public class TutorialManager : Singleton<TutorialManager>
{
    [Header("Tutorial Targets")]
    [SerializeField] private UnitBase targetUndead;
    [SerializeField] private UnitBase targetEnemy;
    [SerializeField] private bool requireTargetMovePosition;
    [SerializeField] private Vector2Int targetMovePosition;

    [Header("Runtime Target Resolve")]
    [SerializeField] private bool resolveTargetsAtRuntime = true;
    [SerializeField] private bool useTargetUndeadType;
    [SerializeField] private UndeadType targetUndeadType;
    [SerializeField] private bool useTargetUndeadGridPosition;
    [SerializeField] private Vector2Int targetUndeadGridPosition;
    [SerializeField] private bool useTargetEnemyType;
    [SerializeField] private EnemyType targetEnemyType;
    [SerializeField] private bool useTargetEnemyGridPosition;
    [SerializeField] private Vector2Int targetEnemyGridPosition;

    [Header("Startup")]
    [FormerlySerializedAs("startOnFirstBattle")]
    [SerializeField] private bool startOnFirstPreparation = true;

    private TutorialStep currentStep = TutorialStep.None;
    private bool hasStarted;
    private bool hasCompleted;

    public bool IsTutorialActive => currentStep != TutorialStep.None && currentStep != TutorialStep.Complete;
    public TutorialStep CurrentStep => currentStep;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Subscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Subscribe<StageChangedEvent>(OnStageChanged);
        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UndeadRelocatedEvent>(OnUndeadRelocated);
        EventBus.Instance.Subscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Subscribe<AttackModeRequestedEvent>(OnAttackModeRequested);
        EventBus.Instance.Subscribe<AttackCompletedEvent>(OnAttackCompleted);
        EventBus.Instance.Subscribe<TurnEndedEvent>(OnTurnEnded);
        EventBus.Instance.Subscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
        EventBus.Instance.Subscribe<VictoryEvent>(OnVictory);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Unsubscribe<WaveClearedEvent>(OnWaveCleared);
        EventBus.Instance.Unsubscribe<StageChangedEvent>(OnStageChanged);
        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UndeadRelocatedEvent>(OnUndeadRelocated);
        EventBus.Instance.Unsubscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Unsubscribe<AttackModeRequestedEvent>(OnAttackModeRequested);
        EventBus.Instance.Unsubscribe<AttackCompletedEvent>(OnAttackCompleted);
        EventBus.Instance.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
        EventBus.Instance.Unsubscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
        EventBus.Instance.Unsubscribe<VictoryEvent>(OnVictory);
    }

    public void StartTutorial()
    {
        if (hasCompleted)
            return;

        hasStarted = true;
        SetStep(TutorialStep.ExplainEnemyDirection);
    }

    public void SetStep(TutorialStep nextStep)
    {
        if (currentStep == nextStep)
            return;

        currentStep = nextStep;
        if (currentStep == TutorialStep.Complete)
        {
            hasCompleted = true;
            hasStarted = false;
            PublishStepChanged();
            SetStep(TutorialStep.None);
            return;
        }

        PublishStepChanged();
    }

    public void ContinueCurrentStep()
    {
        if (currentStep == TutorialStep.ExplainEnemyDirection)
        {
            ResolveTargetsIfNeeded();
            SetStep(TutorialStep.RelocateUndead);
            return;
        }

        if (currentStep == TutorialStep.ExplainCore)
        {
            SetStep(TutorialStep.SelectUndead);
            return;
        }

        if (currentStep == TutorialStep.ExplainVictory)
        {
            SetStep(TutorialStep.Complete);
        }
    }

    public bool CanSelectUnit(UnitBase unit)
    {
        if (!ShouldRestrictInputs())
            return true;

        if (currentStep == TutorialStep.RelocateUndead)
            return true;

        if (currentStep != TutorialStep.SelectUndead)
            return false;

        return targetUndead == null || unit == targetUndead;
    }

    public bool CanMoveTo(Vector2Int gridPosition)
    {
        if (!ShouldRestrictInputs())
            return true;

        if (currentStep != TutorialStep.MoveUnit)
            return false;

        return !requireTargetMovePosition || gridPosition == targetMovePosition;
    }

    public bool CanRelocateUndeadTo(Vector2Int gridPosition)
    {
        if (!ShouldRestrictInputs())
            return true;

        return currentStep == TutorialStep.RelocateUndead;
    }

    public bool CanPressAttackButton()
    {
        if (!ShouldRestrictInputs())
            return true;

        return currentStep == TutorialStep.PressAttackButton;
    }

    public bool CanAttackTarget(UnitBase target)
    {
        if (!ShouldRestrictInputs())
            return true;

        if (currentStep != TutorialStep.AttackEnemy)
            return false;

        return targetEnemy == null || target == targetEnemy;
    }

    public bool CanEndTurn()
    {
        if (!ShouldRestrictInputs())
            return true;

        return currentStep == TutorialStep.EndTurn;
    }

    public bool CanStartBattle()
    {
        if (!ShouldRestrictInputs())
            return true;

        return currentStep == TutorialStep.StartBattle;
    }

    private bool ShouldRestrictInputs()
    {
        return currentStep != TutorialStep.None
            && currentStep != TutorialStep.FreePlay
            && currentStep != TutorialStep.ExplainVictory
            && currentStep != TutorialStep.Complete;
    }

    private void OnStageChanged(StageChangedEvent e)
    {
        if (!startOnFirstPreparation || hasStarted || hasCompleted)
            return;

        if (e.CurrentStage == StageType.Preparation)
        {
            ResolveTargetsIfNeeded();
            StartTutorial();
        }
    }

    private void OnWaveStarted(WaveStartedEvent e)
    {
        ResolveTargetsIfNeeded();

        if (currentStep == TutorialStep.StartBattle)
        {
            SetStep(TutorialStep.ExplainCore);
            return;
        }

        if (!hasStarted && !hasCompleted && e.WaveNumber == 1)
        {
            hasStarted = true;
            SetStep(TutorialStep.ExplainCore);
        }
    }

    private void OnWaveCleared(WaveClearedEvent e)
    {
        if (currentStep == TutorialStep.FreePlay)
            SetStep(TutorialStep.ExplainVictory);
    }

    private void OnUnitSelected(UnitSelectedEvent e)
    {
        if (currentStep == TutorialStep.SelectUndead && IsTargetUndead(e.Unit))
            SetStep(TutorialStep.MoveUnit);
    }

    private void OnUndeadRelocated(UndeadRelocatedEvent e)
    {
        if (currentStep == TutorialStep.RelocateUndead && e.Unit != null && e.From != e.To)
            SetStep(TutorialStep.StartBattle);
    }

    private void OnUnitMoveFinished(UnitMoveFinishedEvent e)
    {
        if (currentStep != TutorialStep.MoveUnit)
            return;

        if (IsTargetUndead(e.Unit) && (!requireTargetMovePosition || e.Unit.GridPosition == targetMovePosition))
            SetStep(TutorialStep.PressAttackButton);
    }

    private void OnAttackModeRequested(AttackModeRequestedEvent e)
    {
        if (currentStep != TutorialStep.PressAttackButton)
            return;

        SetStep(HasAttackableEnemy(e.Action) ? TutorialStep.AttackEnemy : TutorialStep.EndTurn);
    }

    private void OnAttackCompleted(AttackCompletedEvent e)
    {
        if (currentStep == TutorialStep.AttackEnemy && IsTargetUndead(e.Attacker) && IsTargetEnemy(e.Target))
            SetStep(TutorialStep.EndTurn);
    }

    private void OnTurnEnded(TurnEndedEvent e)
    {
        if (currentStep == TutorialStep.EndTurn && e.EndedTurn == TurnType.Player)
            SetStep(TutorialStep.EnemyTurn);
    }

    private void OnPlayerTurnStarted(PlayerTurnStartedEvent e)
    {
        if (currentStep == TutorialStep.EnemyTurn)
            SetStep(TutorialStep.FreePlay);
    }

    private void OnVictory(VictoryEvent e)
    {
        if (currentStep == TutorialStep.FreePlay)
            SetStep(TutorialStep.ExplainVictory);
    }

    private bool IsTargetUndead(UnitBase unit)
    {
        return targetUndead == null || unit == targetUndead;
    }

    private bool IsTargetEnemy(UnitBase unit)
    {
        return targetEnemy == null || unit == targetEnemy;
    }

    private bool HasAttackableEnemy(IUnitAction action)
    {
        if (action == null || UnitRegistry.Instance == null)
            return false;

        var enemies = UnitRegistry.Instance.GetAliveEnemyUnits();
        for (int i = 0; i < enemies.Count; i++)
        {
            UnitBase enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            if (targetEnemy != null && enemy != targetEnemy)
                continue;

            if (action.CanTarget(enemy.GridPosition))
                return true;
        }

        return false;
    }

    private void ResolveTargetsIfNeeded()
    {
        if (!resolveTargetsAtRuntime || UnitRegistry.Instance == null)
            return;

        if (targetUndead == null)
            targetUndead = FindTargetUndead();

        if (targetEnemy == null)
            targetEnemy = FindTargetEnemy();
    }

    private UnitBase FindTargetUndead()
    {
        var undeadUnits = UnitRegistry.Instance.GetAliveUndeadUnits();
        for (int i = 0; i < undeadUnits.Count; i++)
        {
            UnitBase unit = undeadUnits[i];
            if (MatchesUndeadTarget(unit))
                return unit;
        }

        return undeadUnits.Count > 0 ? undeadUnits[0] : null;
    }

    private UnitBase FindTargetEnemy()
    {
        var enemyUnits = UnitRegistry.Instance.GetAliveEnemyUnits();
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            UnitBase unit = enemyUnits[i];
            if (MatchesEnemyTarget(unit))
                return unit;
        }

        return enemyUnits.Count > 0 ? enemyUnits[0] : null;
    }

    private bool MatchesUndeadTarget(UnitBase unit)
    {
        if (unit == null || unit.Team != TeamType.Undead || unit.IsDead)
            return false;

        if (useTargetUndeadGridPosition && unit.GridPosition != targetUndeadGridPosition)
            return false;

        if (useTargetUndeadType)
        {
            UndeadUnit undead = unit as UndeadUnit;
            if (undead == null || undead.UndeadType != targetUndeadType)
                return false;
        }

        return true;
    }

    private bool MatchesEnemyTarget(UnitBase unit)
    {
        if (unit == null || unit.Team != TeamType.Enemy || unit.IsDead)
            return false;

        if (useTargetEnemyGridPosition && unit.GridPosition != targetEnemyGridPosition)
            return false;

        if (useTargetEnemyType)
        {
            EnemyUnit enemy = unit as EnemyUnit;
            if (enemy == null || enemy.EnemyType != targetEnemyType)
                return false;
        }

        return true;
    }

    private void PublishStepChanged()
    {
        UnitBase targetUnit = null;
        bool hasTargetGridPosition = false;

        switch (currentStep)
        {
            case TutorialStep.SelectUndead:
                targetUnit = targetUndead;
                break;
            case TutorialStep.MoveUnit:
                hasTargetGridPosition = requireTargetMovePosition;
                break;
            case TutorialStep.AttackEnemy:
                targetUnit = targetEnemy;
                break;
        }

        EventBus.Instance.Publish(new TutorialStepChangedEvent
        {
            Step = currentStep,
            Message = GetMessage(currentStep),
            TargetUnit = targetUnit,
            HasTargetGridPosition = hasTargetGridPosition,
            TargetGridPosition = targetMovePosition
        });
    }

    private string GetMessage(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.ExplainEnemyDirection:
                return "\uC8FC\uD669\uC0C9\uC73C\uB85C \uD45C\uC2DC\uB41C \uBC29\uD5A5\uC5D0\uC11C \uC801\uC774 \uBAB0\uB824\uC635\uB2C8\uB2E4. \uC804\uD22C \uC804\uC5D0 \uC801\uC758 \uC9C4\uC785 \uBC29\uD5A5\uC744 \uD655\uC778\uD558\uC138\uC694.";
            case TutorialStep.RelocateUndead:
                return "\uC5B8\uB370\uB4DC\uB97C \uAE38\uAC8C \uD074\uB9AD\uD55C \uCC44 \uB4DC\uB798\uADF8\uD574\uC11C \uBC29\uC5B4\uD558\uAE30 \uC88B\uC740 \uC704\uCE58\uB85C \uC62E\uACA8\uBCF4\uC138\uC694.";
            case TutorialStep.StartBattle:
                return "\uBC30\uCE58\uB97C \uB9C8\uCCE4\uB2E4\uBA74 \uC815\uBE44 \uC644\uB8CC \uBC84\uD2BC\uC744 \uB20C\uB7EC \uC804\uD22C\uB97C \uC2DC\uC791\uD558\uC138\uC694.";
            case TutorialStep.ExplainCore:
                return "\uC774\uAC83\uC740 \uC5B8\uB370\uB4DC\uC758 \uD575\uC785\uB2C8\uB2E4. \uC801\uB4E4\uC774 \uD575\uC744 \uD30C\uAD34\uD558\uAE30 \uC804\uC5D0 \uBAA8\uB450 \uB9C9\uC544\uB0B4\uC57C \uD569\uB2C8\uB2E4.";
            case TutorialStep.SelectUndead:
                return "\uBA3C\uC800 \uC5B8\uB370\uB4DC \uC720\uB2DB\uC744 \uC120\uD0DD\uD558\uC138\uC694.";
            case TutorialStep.MoveUnit:
                return requireTargetMovePosition
                    ? "\uD30C\uB780\uC0C9 \uD0C0\uC77C\uC740 \uC774\uB3D9\uD560 \uC218 \uC788\uB294 \uC704\uCE58\uC785\uB2C8\uB2E4. \uAC15\uC870\uB41C \uC704\uCE58\uB85C \uC774\uB3D9\uD558\uC138\uC694."
                    : "\uD30C\uB780\uC0C9 \uD0C0\uC77C\uC740 \uC774\uB3D9\uD560 \uC218 \uC788\uB294 \uC704\uCE58\uC785\uB2C8\uB2E4. \uC6D0\uD558\uB294 \uC704\uCE58\uB85C \uC774\uB3D9\uD558\uC138\uC694.";
            case TutorialStep.PressAttackButton:
                return "\uACF5\uACA9 \uBC84\uD2BC\uC744 \uB20C\uB7EC \uACF5\uACA9 \uAC00\uB2A5\uD55C \uB300\uC0C1\uC744 \uD655\uC778\uD558\uC138\uC694.";
            case TutorialStep.AttackEnemy:
                return "\uBCF4\uB77C\uC0C9 \uD0C0\uC77C \uC548\uC758 \uC801\uC744 \uC120\uD0DD\uD558\uBA74 \uACF5\uACA9\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.";
            case TutorialStep.EndTurn:
                return "\uD589\uB3D9\uC744 \uB9C8\uCCE4\uC2B5\uB2C8\uB2E4. \uD134 \uC885\uB8CC \uBC84\uD2BC\uC744 \uB20C\uB7EC \uC801\uC758 \uD134\uC73C\uB85C \uB118\uAE30\uC138\uC694.";
            case TutorialStep.EnemyTurn:
                return "\uC774\uC81C \uC801\uC774 \uD589\uB3D9\uD569\uB2C8\uB2E4. \uC801\uC740 \uC5B8\uB370\uB4DC\uB098 \uD575\uC744 \uD5A5\uD574 \uC774\uB3D9\uD558\uACE0 \uACF5\uACA9\uD569\uB2C8\uB2E4.";
            case TutorialStep.FreePlay:
                return "\uC774\uC81C \uC9C1\uC811 \uB0A8\uC740 \uC801\uC744 \uCC98\uCE58\uD574\uBCF4\uC138\uC694.";
            case TutorialStep.ExplainVictory:
                return "\uC88B\uC2B5\uB2C8\uB2E4. \uC774\uC81C \uAE30\uBCF8 \uC870\uC791\uC744 \uC775\uD614\uC2B5\uB2C8\uB2E4. \uC55E\uC73C\uB85C\uB294 \uC801\uC758 \uC9C4\uC785 \uBC29\uD5A5\uC744 \uD655\uC778\uD558\uACE0, \uC5B8\uB370\uB4DC\uB97C \uBC30\uCE58\uD574 \uD575\uC744 \uC9C0\uCF1C\uB0B4\uC138\uC694.";
            case TutorialStep.Complete:
                return "\uD29C\uD1A0\uB9AC\uC5BC \uC644\uB8CC!";
            default:
                return string.Empty;
        }
    }
}
