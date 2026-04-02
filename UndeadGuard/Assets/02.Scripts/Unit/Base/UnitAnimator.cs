using UnityEngine;

// 유닛의 Animator를 제어하는 컴포넌트
// 유닛 오브젝트 또는 하위 모델 오브젝트에 부착된 Animator를 찾아 사용한다
public class UnitAnimator : MonoBehaviour
{
    // 파라미터 이름을 해시로 캐싱하여 성능을 높인다
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash    = Animator.StringToHash("Attack");
    private static readonly int DieHash       = Animator.StringToHash("Die");
    private static readonly int HitHash       = Animator.StringToHash("Hit");
    private static readonly int ReviveHash    = Animator.StringToHash("Revive");

    private Animator animator;

    private void Awake()
    {
        // 하위 오브젝트에 Animator가 있는 경우도 처리한다
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning($"{gameObject.name}에서 Animator를 찾을 수 없습니다.");
    }

    // 이동 중 여부를 Animator에 전달한다
    public void SetWalking(bool isWalking)
    {
        if (animator == null) return;
        animator.SetBool(IsWalkingHash, isWalking);
    }

    // 공격 애니메이션을 재생한다
    public void TriggerAttack()
    {
        if (animator == null) return;
        animator.SetTrigger(AttackHash);
    }

    // 피격 애니메이션을 재생한다
    public void TriggerHit()
    {
        if (animator == null) return;
        animator.SetTrigger(HitHash);
    }

    // 사망 애니메이션을 재생한다
    public void TriggerDie()
    {
        if (animator == null) return;
        animator.SetTrigger(DieHash);
    }

    // 부활 애니메이션을 재생한다
    public void TriggerRevive()
    {
        if (animator == null) return;
        animator.SetTrigger(ReviveHash);
    }
}
