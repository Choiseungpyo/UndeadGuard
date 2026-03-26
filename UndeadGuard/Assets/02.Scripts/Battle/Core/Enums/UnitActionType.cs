/// <summary>
/// 유닛이 턴 동안 수행할 수 있는 행동 종류를 정의하는 열거형.
/// 기본 공격, 스킬, 아이템, 대기처럼 행동 1회를 소비하는 선택지를 구분할 때 사용한다.
/// </summary>
public enum UnitActionType
{
    None,
    BasicAttack,
    Skill,
    Item,
    Wait
}