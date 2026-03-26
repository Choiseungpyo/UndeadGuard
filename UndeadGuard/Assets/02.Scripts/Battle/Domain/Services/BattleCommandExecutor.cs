/// <summary>
/// 전투 명령 객체를 실행하는 공용 실행기.
/// 명령의 실행 가능 여부를 확인한 뒤 실제 실행을 호출한다.
/// </summary>
public sealed class BattleCommandExecutor
{
    public bool Execute(IBattleCommand command)
    {
        if (command == null)
        {
            return false;
        }

        if (!command.CanExecute())
        {
            return false;
        }

        return command.Execute();
    }
}