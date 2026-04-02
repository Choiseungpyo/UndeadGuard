using UnityEngine;

// 플레이어가 조종하는 언데드 유닛의 기반 클래스
// 방패병, 전사, 창병, 법사가 이 클래스를 상속받는다
public abstract class UndeadUnit : UnitBase
{
    // 이 언데드 유닛의 종류
    [SerializeField] private UndeadType undeadType;

    // 이 유닛이 보유한 고유 스킬
    // 자식 클래스에서 할당한다
    protected ISkill skill;

    public UndeadType UndeadType => undeadType;

    // 스킬을 사용한다. 행동 완료 상태로 표시된다
    public void UseSkill(Vector2Int targetPosition)
    {
        if (skill == null) return;
        if (!skill.CanUse()) return;
        if (HasActed) return;

        skill.Execute(targetPosition);
        MarkAsActed();
    }

    // 보유 스킬을 반환한다
    public ISkill GetSkill() => skill;
}
