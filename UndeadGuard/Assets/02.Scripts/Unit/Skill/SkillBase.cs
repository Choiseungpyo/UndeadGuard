using UnityEngine;

// Base component for all unit skills.
public abstract class SkillBase : MonoBehaviour, ISkill
{
    [SerializeField] private string skillName;
    [SerializeField] private string description;

    protected UnitBase owner;

    public string SkillName => skillName;
    public string Description => description;

    protected virtual void Awake()
    {
        owner = GetComponent<UnitBase>();
    }

    public virtual bool CanUse()
    {
        if (owner == null) return false;
        if (owner.IsDead) return false;
        if (owner.HasActed) return false;
        return true;
    }

    protected string ResolveSkillActionId()
    {
        if (AttackPatternResolver.TryGetFirstNonBasicActionId(owner, out string fromDatabase))
            return fromDatabase;

        if (!string.IsNullOrWhiteSpace(skillName))
            return skillName.Trim();

        return "Skill";
    }

    protected void PlaySkillParticle(Vector2Int targetPosition)
    {
        if (owner == null)
            return;

        AttackEffectService.Play(owner, targetPosition, ResolveSkillActionId());
    }

    public abstract void Execute(Vector2Int targetPosition);
}
