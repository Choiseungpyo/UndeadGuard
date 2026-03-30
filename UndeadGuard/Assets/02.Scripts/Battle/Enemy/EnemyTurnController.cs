using System;
using System.Collections.Generic;

public sealed class EnemyTurnController : IMoveCompletionReceiver
{
    private readonly BattleState state;
    private readonly EnemyDecisionService decisionService;
    private readonly BattleActionService actionService;
    private readonly BattleCommandExecutor commandExecutor;

    private readonly Queue<int> pendingEnemyIds = new Queue<int>();

    private int currentUnitId = -1;
    private bool isRunning;
    private bool waitingForMoveFinish;

    public event Action TurnFinished;

    public EnemyTurnController(
        BattleState state,
        EnemyDecisionService decisionService,
        BattleActionService actionService,
        BattleCommandExecutor commandExecutor)
    {
        this.state = state;
        this.decisionService = decisionService;
        this.actionService = actionService;
        this.commandExecutor = commandExecutor;
    }

    public void BeginTurn()
    {
        if (isRunning)
        {
            return;
        }

        isRunning = true;
        pendingEnemyIds.Clear();
        currentUnitId = -1;
        waitingForMoveFinish = false;

        foreach (BattleUnit unit in state.GetAliveUnits(TeamType.Enemy))
        {
            pendingEnemyIds.Enqueue(unit.Id);
        }

        TryProcessNextStep();
    }

    public void NotifyMoveFinished(int unitId)
    {
        if (!isRunning || !waitingForMoveFinish)
        {
            return;
        }

        if (currentUnitId != unitId)
        {
            return;
        }

        waitingForMoveFinish = false;
        FinishCurrentUnit();
    }

    private void TryProcessNextStep()
    {
        if (!isRunning || waitingForMoveFinish)
        {
            return;
        }

        while (pendingEnemyIds.Count > 0)
        {
            currentUnitId = pendingEnemyIds.Dequeue();

            if (!state.TryGetUnit(currentUnitId, out BattleUnit unit))
            {
                continue;
            }

            if (TryExecuteMove(currentUnitId))
            {
                return;
            }

            FinishCurrentUnit();
            return;
        }

        CompleteTurn();
    }

    private bool TryExecuteMove(int unitId)
    {
        EnemyMoveDecision decision;
        if (!decisionService.TryGetMoveDecision(unitId, out decision))
        {
            return false;
        }

        MoveCommand command = new MoveCommand(
            state,
            actionService,
            unitId,
            decision.destination,
            decision.reachablePositions,
            decision.path);

        bool executed = commandExecutor.Execute(command);
        if (!executed)
        {
            return false;
        }

        waitingForMoveFinish = true;
        return true;
    }

    private void FinishCurrentUnit()
    {
        currentUnitId = -1;
        TryProcessNextStep();
    }

    private void CompleteTurn()
    {
        isRunning = false;
        currentUnitId = -1;
        waitingForMoveFinish = false;
        TurnFinished?.Invoke();
    }
}