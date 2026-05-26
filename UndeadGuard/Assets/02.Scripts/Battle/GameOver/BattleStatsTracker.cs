using System.Collections.Generic;
using UnityEngine;

public class BattleStatsTracker : MonoBehaviour
{
    [SerializeField] private int killedEnemyCount;
    [SerializeField] private int lostUndeadCount;
    [SerializeField] private int revivedUndeadCount;
    [SerializeField] private int totalCoreDamageTaken;
    [SerializeField] private string lastAttackerName = "Unknown";

    private readonly Dictionary<UnitBase, int> undeadKillCounts = new Dictionary<UnitBase, int>();
    private readonly Dictionary<UnitBase, int> undeadDamageTotals = new Dictionary<UnitBase, int>();
    private readonly Dictionary<EnemyUnit, UnitBase> lastUndeadAttackerByEnemy = new Dictionary<EnemyUnit, UnitBase>();

    public int KilledEnemyCount => killedEnemyCount;
    public int LostUndeadCount => lostUndeadCount;
    public int RevivedUndeadCount => revivedUndeadCount;
    public int TotalCoreDamageTaken => totalCoreDamageTaken;
    public string LastAttackerName => string.IsNullOrWhiteSpace(lastAttackerName) ? "Unknown" : lastAttackerName;

    private void Awake()
    {
        ResetStats();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Subscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Instance.Subscribe<UnitRevivedEvent>(OnUnitRevived);
        EventBus.Instance.Subscribe<CoreDamagedEvent>(OnCoreDamaged);
        EventBus.Instance.Subscribe<UnitAttackedEvent>(OnUnitAttacked);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Instance.Unsubscribe<UnitRevivedEvent>(OnUnitRevived);
        EventBus.Instance.Unsubscribe<CoreDamagedEvent>(OnCoreDamaged);
        EventBus.Instance.Unsubscribe<UnitAttackedEvent>(OnUnitAttacked);
    }

    public GameOverResultData CreateGameOverResultData(int survivedDay, int reachedWave)
    {
        return new GameOverResultData
        {
            survivedDay = Mathf.Max(1, survivedDay),
            reachedWave = Mathf.Max(1, reachedWave),
            killedEnemyCount = Mathf.Max(0, killedEnemyCount),
            lostUndeadCount = Mathf.Max(0, lostUndeadCount),
            revivedUndeadCount = Mathf.Max(0, revivedUndeadCount),
            totalCoreDamageTaken = Mathf.Max(0, totalCoreDamageTaken),
            lastAttackerName = LastAttackerName
        };
    }

    public BattleVictoryResultData CreateVictoryResultData(
        int clearedWave,
        int totalWaves,
        int totalTurns,
        int survivingUndeadCount,
        int remainingCoreHp,
        int maxCoreHp,
        int usedDarkEnergy)
    {
        int topKillerKillCount = ResolveTopKillerKillCount();
        string topKillerName = topKillerKillCount > 0 ? ResolveTopKillerUnitName() : "\uAE30\uB85D \uC5C6\uC74C";

        return new BattleVictoryResultData
        {
            clearedWave = Mathf.Max(0, clearedWave),
            totalWaves = Mathf.Max(0, totalWaves),
            totalTurns = Mathf.Max(0, totalTurns),
            survivingUndeadCount = Mathf.Max(0, survivingUndeadCount),
            lostUndeadCount = Mathf.Max(0, lostUndeadCount),
            killedEnemyCount = Mathf.Max(0, killedEnemyCount),
            remainingCoreHp = Mathf.Max(0, remainingCoreHp),
            maxCoreHp = Mathf.Max(0, maxCoreHp),
            usedDarkEnergy = Mathf.Max(0, usedDarkEnergy),
            topKillerUnitName = topKillerName,
            mvpUnitName = topKillerName,
            MvpUndeadName = topKillerName,
            MvpUndeadKillCount = topKillerKillCount
        };
    }

    private void ResetStats()
    {
        killedEnemyCount = 0;
        lostUndeadCount = 0;
        revivedUndeadCount = 0;
        totalCoreDamageTaken = 0;
        lastAttackerName = "Unknown";
        undeadKillCounts.Clear();
        undeadDamageTotals.Clear();
        lastUndeadAttackerByEnemy.Clear();
    }

    private void OnUnitAttacked(UnitAttackedEvent e)
    {
        if (e == null || e.Attacker == null || e.Target == null)
            return;

        if (e.Attacker.Team != TeamType.Undead || e.Target.Team != TeamType.Enemy)
            return;

        int damage = Mathf.Max(0, e.Damage);
        if (damage > 0)
        {
            if (!undeadDamageTotals.ContainsKey(e.Attacker))
                undeadDamageTotals[e.Attacker] = 0;

            undeadDamageTotals[e.Attacker] += damage;
        }

        EnemyUnit enemy = e.Target as EnemyUnit;
        if (enemy != null)
            lastUndeadAttackerByEnemy[enemy] = e.Attacker;
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        if (e == null || e.Unit == null)
            return;

        killedEnemyCount++;

        if (lastUndeadAttackerByEnemy.TryGetValue(e.Unit, out UnitBase killer) && killer != null)
        {
            if (!undeadKillCounts.ContainsKey(killer))
                undeadKillCounts[killer] = 0;

            undeadKillCounts[killer]++;
        }

        lastUndeadAttackerByEnemy.Remove(e.Unit);
    }

    private void OnUnitDied(UnitDiedEvent e)
    {
        if (e == null || e.Unit == null)
            return;

        if (e.Unit.Team == TeamType.Undead)
            lostUndeadCount++;
    }

    private void OnUnitRevived(UnitRevivedEvent e)
    {
        if (e == null || e.Unit == null)
            return;

        if (e.Unit.Team == TeamType.Undead)
            revivedUndeadCount++;
    }

    private void OnCoreDamaged(CoreDamagedEvent e)
    {
        if (e == null)
            return;

        totalCoreDamageTaken += Mathf.Max(0, e.DamageAmount);

        if (!string.IsNullOrWhiteSpace(e.AttackerName))
            lastAttackerName = e.AttackerName;
    }

    private string ResolveTopKillerUnitName()
    {
        UnitBase bestUnit = null;
        int bestKillCount = -1;

        foreach (var pair in undeadKillCounts)
        {
            if (pair.Key == null)
                continue;

            if (pair.Value > bestKillCount)
            {
                bestUnit = pair.Key;
                bestKillCount = pair.Value;
            }
        }

        return bestUnit != null ? ResolveUnitDisplayName(bestUnit) : "\uAE30\uB85D \uC5C6\uC74C";
    }

    private int ResolveTopKillerKillCount()
    {
        int bestKillCount = 0;
        foreach (var pair in undeadKillCounts)
        {
            if (pair.Key == null)
                continue;

            if (pair.Value > bestKillCount)
                bestKillCount = pair.Value;
        }

        return Mathf.Max(0, bestKillCount);
    }

    private string ResolveMvpUnitName()
    {
        return ResolveTopKillerUnitName();
    }

    private static string ResolveUnitDisplayName(UnitBase unit)
    {
        if (unit == null)
            return "\uAE30\uB85D \uC5C6\uC74C";

        if (unit is UndeadUnit undead)
        {
            switch (undead.UndeadType)
            {
                case UndeadType.Shield:
                    return "\uD574\uACE8 \uBC29\uD328\uBCD1";
                case UndeadType.Warrior:
                    return "\uD574\uACE8 \uC804\uC0AC";
                case UndeadType.Spearman:
                    return "\uD574\uACE8 \uCC3D\uBCD1";
                case UndeadType.Mage:
                    return "\uC5B8\uB370\uB4DC \uBC95\uC0AC";
            }
        }

        string rawName = unit.name ?? string.Empty;
        int cloneIndex = rawName.IndexOf('(');
        if (cloneIndex >= 0)
            rawName = rawName.Substring(0, cloneIndex).Trim();

        return string.IsNullOrWhiteSpace(rawName) ? "\uC774\uB984 \uC5C6\uB294 \uC720\uB2DB" : rawName;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DebugAddEnemyKill(int count = 1)
    {
        killedEnemyCount += Mathf.Max(0, count);
    }

    public void DebugAddUndeadDeath(int count = 1)
    {
        lostUndeadCount += Mathf.Max(0, count);
    }

    public void DebugAddUndeadRevive(int count = 1)
    {
        revivedUndeadCount += Mathf.Max(0, count);
    }

    public void DebugAddCoreDamage(int damage, string attackerName)
    {
        totalCoreDamageTaken += Mathf.Max(0, damage);
        if (!string.IsNullOrWhiteSpace(attackerName))
            lastAttackerName = attackerName;
    }
#endif
}
