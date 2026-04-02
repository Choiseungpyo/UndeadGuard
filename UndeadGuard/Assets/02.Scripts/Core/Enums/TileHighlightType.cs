// 타일에 표시할 하이라이트 종류를 정의한다
public enum TileHighlightType
{
    // 하이라이트 없음
    None,

    // 유닛이 이동할 수 있는 칸
    Movable,

    // 유닛이 공격할 수 있는 칸
    Attackable,

    // 마우스 오버 시 표시되는 이동 경로 칸
    Path,

    // 이동 목적지로 강조되는 칸
    Destination,

    // 현재 선택된 유닛이 위치한 칸
    Selected
}
