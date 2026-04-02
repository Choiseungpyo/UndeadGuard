using UnityEngine;

// 법사 유닛
// 체력은 낮지만 공격력이 높고 사거리가 길다
// 스킬: 마법 공격 - 지정한 타일 중심 3x3 범위에 마법 피해를 준다
public class Mage : UndeadUnit
{
    protected override void Awake()
    {
        base.Awake();
        skill = GetComponent<MagicAttackSkill>();
    }
}
