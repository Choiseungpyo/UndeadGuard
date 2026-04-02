using UnityEngine;

// 모든 스킬의 공통 기반 클래스
// 유닛 오브젝트에 컴포넌트로 부착하여 사용한다
public abstract class SkillBase : MonoBehaviour, ISkill
{
    [SerializeField] private string skillName;
    [SerializeField] private string description;

    // 이 스킬을 보유한 유닛
    protected UnitBase owner;

    public string SkillName => skillName;
    public string Description => description;

    protected virtual void Awake()
    {
        owner = GetComponent<UnitBase>();
    }

    // 스킬을 사용할 수 있는지 여부를 반환한다. 자식 클래스에서 조건을 추가할 수 있다
    public virtual bool CanUse()
    {
        if (owner == null) return false;
        if (owner.IsDead) return false;
        if (owner.HasActed) return false;
        return true;
    }

    // 지정한 타일 위치에 스킬 효과를 적용한다. 자식 클래스에서 구현한다
    public abstract void Execute(Vector2Int targetPosition);
}
