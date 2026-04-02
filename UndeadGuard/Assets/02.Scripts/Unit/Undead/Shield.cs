using UnityEngine;

// 방패병 유닛
// 체력과 방어력이 높으며 전열을 유지하고 아군을 보호하는 역할을 한다
// 스킬: 도발 - 전방 3x3 범위의 적에게 도발 상태를 부여한다
public class Shield : UndeadUnit
{
    protected override void Awake()
    {
        base.Awake();
        skill = GetComponent<TauntSkill>();
    }
}
