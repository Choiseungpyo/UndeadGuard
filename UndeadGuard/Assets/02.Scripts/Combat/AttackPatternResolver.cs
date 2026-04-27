using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AttackPatternResolver
{
    private const string ResourcePath = "Combat/AttackPatternDatabase";

#if UNITY_EDITOR
    private static readonly string[] EditorFallbackAssetPaths =
    {
        "Assets/Resources/Combat/AttackPatternDatabase.asset",
        "Assets/02.Scripts/AttackPatternDatabase.asset"
    };
#endif

    private static AttackPatternDatabase cachedDatabase;
    private static bool hasTriedLoad;
    private static bool hasLoggedMissingDatabase;

    public static AttackPatternDatabase Database
    {
        get
        {
            EnsureDatabaseLoaded();
            return cachedDatabase;
        }
    }

    public static void SetDatabase(AttackPatternDatabase database)
    {
        cachedDatabase = database;
        hasTriedLoad = true;
        hasLoggedMissingDatabase = false;
    }

    public static bool IsTargetInRange(UnitBase attacker, Vector2Int targetPosition, string actionId, int fallbackRange)
    {
        if (attacker == null)
            return false;

        Vector2Int from = attacker.GridPosition;
        List<Vector2Int> offsets = GetRelativeTargetOffsets(attacker, actionId, fallbackRange);

        Vector2Int delta = targetPosition - from;
        for (int i = 0; i < offsets.Count; i++)
        {
            if (offsets[i] == delta)
                return true;
        }

        return false;
    }

    public static bool TryGetPattern(UnitBase attacker, string actionId, out AttackPatternEntry entry)
    {
        entry = null;
        if (attacker == null || Database == null)
            return false;

        return Database.TryGetPattern(attacker, actionId, out entry);
    }

    public static List<Vector2Int> GetTargetTiles(UnitBase attacker, string actionId, int fallbackRange)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        if (attacker == null)
            return tiles;

        Vector2Int center = attacker.GridPosition;
        List<Vector2Int> offsets = GetRelativeTargetOffsets(attacker, actionId, fallbackRange);

        for (int i = 0; i < offsets.Count; i++)
            tiles.Add(center + offsets[i]);

        return tiles;
    }

    public static List<Vector2Int> GetAttackOriginCandidates(UnitBase attacker, Vector2Int targetPosition, string actionId, int fallbackRange)
    {
        List<Vector2Int> origins = new List<Vector2Int>();
        if (attacker == null)
            return origins;

        HashSet<Vector2Int> dedupe = new HashSet<Vector2Int>();
        List<Vector2Int> offsets = GetRelativeTargetOffsets(attacker, actionId, fallbackRange);

        for (int i = 0; i < offsets.Count; i++)
        {
            Vector2Int origin = targetPosition - offsets[i];
            if (dedupe.Add(origin))
                origins.Add(origin);
        }

        return origins;
    }

    public static List<Vector2Int> GetRelativeTargetOffsets(UnitBase attacker, string actionId, int fallbackRange)
    {
        if (TryGetPattern(attacker, actionId, out AttackPatternEntry entry) && entry != null)
        {
            List<Vector2Int> custom = entry.RelativeTargetOffsets;
            if (custom != null && custom.Count > 0)
                return new List<Vector2Int>(custom);

            return BuildDiamondOffsets(entry.MaxRange);
        }

        return BuildDiamondOffsets(fallbackRange);
    }

    public static bool TryGetFirstNonBasicActionId(UnitBase unit, out string actionId)
    {
        actionId = null;

        if (unit == null || Database == null)
            return false;

        List<UnitAttackPatternProfile> profiles = Database.Profiles;
        for (int i = 0; i < profiles.Count; i++)
        {
            UnitAttackPatternProfile profile = profiles[i];
            if (profile == null || !profile.Matches(unit))
                continue;

            List<AttackPatternEntry> patterns = profile.Patterns;
            for (int j = 0; j < patterns.Count; j++)
            {
                AttackPatternEntry entry = patterns[j];
                if (entry == null)
                    continue;

                string id = string.IsNullOrWhiteSpace(entry.ActionId)
                    ? AttackActionIds.BasicAttack
                    : entry.ActionId.Trim();

                if (!string.Equals(id, AttackActionIds.BasicAttack, System.StringComparison.OrdinalIgnoreCase))
                {
                    actionId = id;
                    return true;
                }
            }

            break;
        }

        return false;
    }

    private static List<Vector2Int> BuildDiamondOffsets(int range)
    {
        int clampedRange = Mathf.Max(0, range);
        List<Vector2Int> offsets = new List<Vector2Int>();

        for (int dx = -clampedRange; dx <= clampedRange; dx++)
        {
            for (int dy = -clampedRange; dy <= clampedRange; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > clampedRange)
                    continue;

                if (dx == 0 && dy == 0)
                    continue;

                offsets.Add(new Vector2Int(dx, dy));
            }
        }

        return offsets;
    }

    private static void EnsureDatabaseLoaded()
    {
        if (hasTriedLoad)
            return;

        cachedDatabase = Resources.Load<AttackPatternDatabase>(ResourcePath);

#if UNITY_EDITOR
        if (cachedDatabase == null)
            cachedDatabase = TryLoadDatabaseFromEditorAssets();
#endif

        hasTriedLoad = true;

        if (cachedDatabase == null && !hasLoggedMissingDatabase)
        {
            hasLoggedMissingDatabase = true;
            Debug.LogWarning("AttackPatternResolver: AttackPatternDatabase not found. Using fallback diamond range from UnitStats.AttackRange.");
        }
    }

#if UNITY_EDITOR
    private static AttackPatternDatabase TryLoadDatabaseFromEditorAssets()
    {
        for (int i = 0; i < EditorFallbackAssetPaths.Length; i++)
        {
            string path = EditorFallbackAssetPaths[i];
            AttackPatternDatabase database = AssetDatabase.LoadAssetAtPath<AttackPatternDatabase>(path);
            if (database != null)
                return database;
        }

        string[] guids = AssetDatabase.FindAssets("t:AttackPatternDatabase");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            AttackPatternDatabase database = AssetDatabase.LoadAssetAtPath<AttackPatternDatabase>(path);
            if (database != null)
                return database;
        }

        return null;
    }
#endif
}
