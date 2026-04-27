using UnityEngine;

public class UnitAnimator : MonoBehaviour
{
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int ReviveHash = Animator.StringToHash("Revive");

    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning($"{gameObject.name}: Animator component not found.");
    }

    public void SetWalking(bool isWalking)
    {
        if (animator == null) return;
        animator.SetBool(IsWalkingHash, isWalking);
    }

    public void TriggerAttack()
    {
        if (animator == null) return;
        animator.SetTrigger(AttackHash);
    }

    public void TriggerHit()
    {
        if (animator == null) return;
        animator.SetTrigger(HitHash);
    }

    public void TriggerDie()
    {
        if (animator == null) return;
        animator.SetTrigger(DieHash);
    }

    public void TriggerRevive()
    {
        if (animator == null) return;
        animator.SetTrigger(ReviveHash);
    }
}
