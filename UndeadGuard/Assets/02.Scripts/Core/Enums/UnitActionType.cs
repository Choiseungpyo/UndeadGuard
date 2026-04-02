// 유닛이 턴 중에 수행할 수 있는 행동 종류를 정의한다
public enum UnitActionType
{
    // 아직 행동을 선택하지 않은 상태
    None,

    // 지정한 타일로 이동한다
    Move,

    // 적 유닛을 공격한다
    Attack,

    // 고유 스킬을 사용한다
    Skill,

    // 아이템을 사용한다
    Item,

    // 이번 턴 행동을 건너뛴다
    Wait
}
