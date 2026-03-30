using System.Linq;
using UnityEngine;

public sealed class BattleTurnFlow : MonoBehaviour
{
    private BattleState state;
    private TurnManager turnManager;
    private PlayerCommandController playerCommandController;
    private BattleInputController battleInputController;
    private BattleCommandExecutor commandExecutor;
    private EnemyTurnController enemyTurnController;
    private BattlePresenter presenter;

    private bool isBattleFinished;

    public void Initialize(
        BattleState state,
        TurnManager turnManager,
        PlayerCommandController playerCommandController,
        BattleInputController battleInputController,
        BattleCommandExecutor commandExecutor,
        EnemyTurnController enemyTurnController,
        BattlePresenter presenter)
    {
        this.state = state;
        this.turnManager = turnManager;
        this.playerCommandController = playerCommandController;
        this.battleInputController = battleInputController;
        this.commandExecutor = commandExecutor;
        this.enemyTurnController = enemyTurnController;
        this.presenter = presenter;

        turnManager.TurnStarted += HandleTurnStarted;
        enemyTurnController.TurnFinished += HandleEnemyTurnFinished;
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted -= HandleTurnStarted;
        }

        if (enemyTurnController != null)
        {
            enemyTurnController.TurnFinished -= HandleEnemyTurnFinished;
        }
    }

    public void StartBattle()
    {
        isBattleFinished = false;
        turnManager.StartBattle(state);
    }

    public bool CanEndPlayerTurn()
    {
        if (isBattleFinished)
        {
            return false;
        }

        if (turnManager == null)
        {
            return false;
        }

        if (turnManager.CurrentTurn != TeamType.Player)
        {
            return false;
        }

        if (playerCommandController == null)
        {
            return false;
        }

        if (playerCommandController.IsActing)
        {
            return false;
        }

        return true;
    }

    public void RequestEndPlayerTurn()
    {
        if (!CanEndPlayerTurn())
        {
            return;
        }

        playerCommandController.ClearSelection();

        EndTurnCommand command = new EndTurnCommand(state, turnManager);
        commandExecutor.Execute(command);
    }

    private void HandleTurnStarted(TeamType team, int round)
    {
        if (isBattleFinished)
        {
            return;
        }

        if (CheckBattleEnd())
        {
            return;
        }

        if (team == TeamType.Player)
        {
            if (presenter != null)
            {
                presenter.SetMoveCompletionReceiver(playerCommandController);
            }

            playerCommandController.SetPlayerTurn(true);
            battleInputController.SetInputEnabled(true);
            return;
        }

        if (presenter != null)
        {
            presenter.SetMoveCompletionReceiver(enemyTurnController);
        }

        playerCommandController.SetPlayerTurn(false);
        battleInputController.SetInputEnabled(false);

        enemyTurnController.BeginTurn();
    }

    private void HandleEnemyTurnFinished()
    {
        if (isBattleFinished)
        {
            return;
        }

        if (CheckBattleEnd())
        {
            return;
        }

        turnManager.EndCurrentTurn(state);
    }

    private bool CheckBattleEnd()
    {
        if (state == null)
        {
            return false;
        }

        if (state.Core != null && state.Core.IsDestroyed)
        {
            isBattleFinished = true;
            playerCommandController.SetPlayerTurn(false);
            battleInputController.SetInputEnabled(false);
            Debug.Log("Defeat");
            return true;
        }

        if (!state.GetAliveUnits(TeamType.Enemy).Any())
        {
            isBattleFinished = true;
            playerCommandController.SetPlayerTurn(false);
            battleInputController.SetInputEnabled(false);
            Debug.Log("Wave Complete");
            return true;
        }

        return false;
    }
}