using UnityEngine;

// 유닛이 보유한 스킬이 구현해야 하는 인터페이스
public interface ISkill
{
    // 스킬 이름
    string SkillName { get; }

    // 스킬 설명
    string Description { get; }

    // 스킬을 사용할 수 있는지 여부를 반환한다
    bool CanUse();

    // 지정한 타일 위치에 스킬을 실행한다
    void Execute(Vector2Int targetPosition);
}
