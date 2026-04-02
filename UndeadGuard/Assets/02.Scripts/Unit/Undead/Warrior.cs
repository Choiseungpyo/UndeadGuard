using UnityEngine;

// 전사 유닛
// 전반적으로 균형 잡힌 기본형 근접 딜러다
// 스킬: 칼로 베기 - 전방 부채형 범위로 근접 피해를 준다
public class Warrior : UndeadUnit
{
    protected override void Awake()
    {
        base.Awake();
        skill = GetComponent<SlashSkill>();
    }
}
