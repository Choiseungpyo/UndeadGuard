using System;
using System.Collections.Generic;
using UnityEngine;

public static class UnitActionIds
{
    public const string DefaultAction = "DefaultAction";
}

public enum AttackParticleSpawnMode
{
    None,
    TargetCell,
    InFrontOfTarget,
    CustomOffsetFromAttacker
}

[Serializable]
public class AttackPatternEntry
{
    [SerializeField] private string actionId = UnitActionIds.DefaultAction;
    [SerializeField] private int maxRange = 1;
    [SerializeField] private List<Vector2Int> relativeTargetOffsets = new List<Vector2Int>();
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private AttackParticleSpawnMode particleSpawnMode = AttackParticleSpawnMode.None;
    [SerializeField] private Vector2Int particleGridOffset = Vector2Int.zero;
    [SerializeField] private Vector3 particleWorldOffset = Vector3.zero;
    [SerializeField] private float particleSpawnDelay = 0.12f;
    [SerializeField] private float particleAutoDestroyDelay = 3f;

    public string ActionId
    {
        get => actionId;
        set => actionId = string.IsNullOrWhiteSpace(value) ? UnitActionIds.DefaultAction : value.Trim();
    }

    public int MaxRange
    {
        get => maxRange;
        set => maxRange = Mathf.Max(0, value);
    }

    public List<Vector2Int> RelativeTargetOffsets => relativeTargetOffsets;

    public GameObject ParticlePrefab
    {
        get => particlePrefab;
        set => particlePrefab = value;
    }

    public AttackParticleSpawnMode ParticleSpawnMode
    {
        get => particleSpawnMode;
        set => particleSpawnMode = value;
    }

    public Vector2Int ParticleGridOffset
    {
        get => particleGridOffset;
        set => particleGridOffset = value;
    }

    public Vector3 ParticleWorldOffset
    {
        get => particleWorldOffset;
        set => particleWorldOffset = value;
    }

    public float ParticleSpawnDelay
    {
        get => particleSpawnDelay;
        set => particleSpawnDelay = Mathf.Max(0f, value);
    }

    public float ParticleAutoDestroyDelay
    {
        get => particleAutoDestroyDelay;
        set => particleAutoDestroyDelay = Mathf.Max(0f, value);
    }
}

[Serializable]
public class UnitAttackPatternProfile
{
    [SerializeField] private TeamType team = TeamType.Undead;
    [SerializeField] private UndeadType undeadType;
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private List<AttackPatternEntry> patterns = new List<AttackPatternEntry>();

    public TeamType Team
    {
        get => team;
        set => team = value;
    }

    public UndeadType UndeadType
    {
        get => undeadType;
        set => undeadType = value;
    }

    public EnemyType EnemyType
    {
        get => enemyType;
        set => enemyType = value;
    }

    public List<AttackPatternEntry> Patterns => patterns;

    public bool Matches(UnitBase unit)
    {
        if (unit == null || unit.Team != team)
            return false;

        if (team == TeamType.Undead)
        {
            UndeadUnit undead = unit as UndeadUnit;
            return undead != null && undead.UndeadType == undeadType;
        }

        if (team == TeamType.Enemy)
        {
            EnemyUnit enemy = unit as EnemyUnit;
            return enemy != null && enemy.EnemyType == enemyType;
        }

        return false;
    }
}

[CreateAssetMenu(fileName = "AttackPatternDatabase", menuName = "Game/Combat/Attack Pattern Database")]
public class AttackPatternDatabase : ScriptableObject
{
    [SerializeField] private List<UnitAttackPatternProfile> profiles = new List<UnitAttackPatternProfile>();

    public List<UnitAttackPatternProfile> Profiles => profiles;

    public bool TryGetPattern(UnitBase unit, string actionId, out AttackPatternEntry pattern)
    {
        pattern = null;
        if (unit == null)
            return false;

        string normalizedActionId = NormalizeActionId(actionId);

        for (int i = 0; i < profiles.Count; i++)
        {
            UnitAttackPatternProfile profile = profiles[i];
            if (profile == null || !profile.Matches(unit))
                continue;

            List<AttackPatternEntry> entries = profile.Patterns;
            for (int j = 0; j < entries.Count; j++)
            {
                AttackPatternEntry entry = entries[j];
                if (entry == null)
                    continue;

                if (string.Equals(NormalizeActionId(entry.ActionId), normalizedActionId, StringComparison.OrdinalIgnoreCase))
                {
                    pattern = entry;
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    private static string NormalizeActionId(string actionId)
    {
        return string.IsNullOrWhiteSpace(actionId)
            ? UnitActionIds.DefaultAction
            : actionId.Trim();
    }
}
