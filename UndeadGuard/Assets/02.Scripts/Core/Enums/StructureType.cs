// 맵 타일에 배치할 수 있는 구조물 종류를 정의한다
// MapDefinition과 MapEditor에서 타일 속성을 지정할 때 사용한다
public enum StructureType
{
    // 배치된 구조물이 없는 빈 칸
    None,

    // 이동이 불가능한 벽
    Wall,

    // 방어 목표인 언데드 핵
    Core,

    // 죽은 언데드 유닛을 부활시키는 제단
    RevivalAltar
}
