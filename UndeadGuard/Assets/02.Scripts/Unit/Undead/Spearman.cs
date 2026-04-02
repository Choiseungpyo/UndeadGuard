using UnityEngine;

// 창병 유닛
// 전사보다 사거리가 길며 전열을 견제하는 역할을 한다
// 스킬: 찌르기 - 전방 직선 3칸을 관통하여 피해를 준다
public class Spearman : UndeadUnit
{
    protected override void Awake()
    {
        base.Awake();
        skill = GetComponent<ThrustSkill>();
    }
}
