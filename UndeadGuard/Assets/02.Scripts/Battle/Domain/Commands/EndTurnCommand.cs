/// <summary>
/// 플레이어 턴 종료 행동을 표현하는 명령 객체.
/// 현재 전투 상태와 턴 시스템을 이용해 턴 전환을 실행한다.
/// </summary>
public sealed class EndTurnCommand : IBattleCommand
{
    private readonly BattleState state;
    private readonly TurnSystem turnSystem;

    public EndTurnCommand(BattleState state, TurnSystem turnSystem)
    {
        this.state = state;
        this.turnSystem = turnSystem;
    }

    public bool CanExecute()
    {
        if (state == null || turnSystem == null)
        {
            return false;
        }

        return turnSystem.CurrentTurn == TeamType.Player;
    }

    public bool Execute()
    {
        turnSystem.EndCurrentTurn(state);
        return true;
    }
}