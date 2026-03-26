/// <summary>
/// 전투에서 실행할 행동 명령의 공통 인터페이스.
/// 이동, 턴 종료, 공격 등 각 행동을 독립된 명령 객체로 다루기 위해 사용한다.
/// </summary>
public interface IBattleCommand
{
    bool CanExecute();
    bool Execute();
}