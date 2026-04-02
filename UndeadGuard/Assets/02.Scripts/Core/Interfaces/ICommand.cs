// 사령 명령이 구현해야 하는 인터페이스
// 플레이어가 사령 포인트를 소모해 특정 유닛에게 부여하는 특수 행동을 정의한다
public interface ICommand
{
    // 명령 이름
    string CommandName { get; }

    // 명령 설명
    string Description { get; }

    // 사령 포인트 소모 비용
    int Cost { get; }

    // 해당 유닛에게 명령을 적용할 수 있는지 여부를 반환한다
    bool CanExecute(UnitBase target);

    // 해당 유닛에게 명령 효과를 적용한다
    void Execute(UnitBase target);
}
