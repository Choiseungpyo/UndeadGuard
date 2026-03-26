using System;

/// <summary>
/// 전투의 턴 흐름을 관리하는 클래스.
/// 현재 턴 진영과 라운드를 관리하고,
/// 턴 시작 시 유닛 행동 상태를 초기화한다.
/// </summary>
public sealed class TurnSystem
{
    public TeamType CurrentTurn { get; private set; }
    public int Round { get; private set; }

    public event Action<TeamType, int> TurnStarted;
    public event Action<TeamType, int> TurnEnded;

    public TurnSystem()
    {
        CurrentTurn = TeamType.Player;
        Round = 1;
    }

    public void StartBattle(BattleState state)
    {
        BeginTurn(state, TeamType.Player);
    }

    public void EndCurrentTurn(BattleState state)
    {
        TeamType endedTurn = CurrentTurn;
        int endedRound = Round;

        TurnEnded?.Invoke(endedTurn, endedRound);

        if (CurrentTurn == TeamType.Player)
        {
            BeginTurn(state, TeamType.Enemy);
        }
        else
        {
            Round++;
            BeginTurn(state, TeamType.Player);
        }
    }

    private void BeginTurn(BattleState state, TeamType team)
    {
        CurrentTurn = team;

        foreach (BattleUnit unit in state.GetAliveUnits(team))
        {
            unit.ResetTurnFlags();
        }

        TurnStarted?.Invoke(CurrentTurn, Round);
    }
}