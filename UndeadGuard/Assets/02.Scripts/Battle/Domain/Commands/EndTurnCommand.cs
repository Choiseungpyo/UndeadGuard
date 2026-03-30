/// <summary>
/// 플레이어 턴 종료 행동을 표현하는 명령 객체.
/// 현재 전투 상태와 턴 관리자를 이용해 턴 전환을 실행한다.
/// </summary>
public sealed class EndTurnCommand : IBattleCommand
{
    private readonly BattleState state;
    private readonly TurnManager turnManager;

    public EndTurnCommand(BattleState state, TurnManager turnManager)
    {
        this.state = state;
        this.turnManager = turnManager;
    }

    public bool CanExecute()
    {
        if (state == null || turnManager == null)
        {
            return false;
        }

        return turnManager.CurrentTurn == TeamType.Player;
    }

    public bool Execute()
    {
        turnManager.EndCurrentTurn(state);
        return true;
    }
}