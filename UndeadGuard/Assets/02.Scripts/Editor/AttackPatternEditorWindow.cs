#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class AttackPatternEditorWindow : EditorWindow
{
    private enum CharacterFactionTab
    {
        Undead,
        Zombie
    }

    private AttackPatternDatabase database;

    private readonly List<UndeadType> undeadOptions = new List<UndeadType>();
    private readonly List<EnemyType> zombieOptions = new List<EnemyType>();

    private CharacterFactionTab selectedFaction = CharacterFactionTab.Undead;
    private int selectedUndeadIndex;
    private int selectedZombieIndex;
    private int selectedPatternIndex;

    private Vector2 windowScroll;
    private Vector2 gridScroll;

    [MenuItem("Tools/Combat/Attack Pattern Editor")]
    public static void Open()
    {
        GetWindow<AttackPatternEditorWindow>("Attack Pattern Editor");
    }

    private void OnEnable()
    {
        BuildCharacterOptions();
    }

    private void OnGUI()
    {
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);

        DrawHeader();
        EditorGUILayout.Space(8f);

        if (database == null)
        {
            EditorGUILayout.HelpBox("Assign or create an AttackPatternDatabase asset.", MessageType.Info);
            if (GUILayout.Button("Create New AttackPatternDatabase"))
                CreateNewDatabase();

            EditorGUILayout.EndScrollView();
            return;
        }

        if (undeadOptions.Count == 0 || zombieOptions.Count == 0)
            BuildCharacterOptions();

        DrawCharacterSection();
        EditorGUILayout.Space(8f);

        UnitAttackPatternProfile profile = GetOrCreateSelectedProfile();
        if (profile == null)
        {
            EditorGUILayout.HelpBox("Could not resolve selected character profile.", MessageType.Error);
            EditorGUILayout.EndScrollView();
            return;
        }

        NormalizeProfile(profile);
        DrawActionSection(profile);

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Attack Pattern Editor", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        database = (AttackPatternDatabase)EditorGUILayout.ObjectField(
            "Database", database, typeof(AttackPatternDatabase), false);
        if (EditorGUI.EndChangeCheck())
        {
            AttackPatternResolver.SetDatabase(database);
            selectedPatternIndex = 0;
        }
    }

    private void DrawCharacterSection()
    {
        DrawFactionSelector();

        if (GetSelectedTeam() == TeamType.Undead)
        {
            if (undeadOptions.Count == 0)
            {
                EditorGUILayout.HelpBox("No Undead types found.", MessageType.Warning);
                return;
            }

            string[] labels = BuildUndeadLabels();
            selectedUndeadIndex = Mathf.Clamp(selectedUndeadIndex, 0, Mathf.Max(0, labels.Length - 1));

            EditorGUI.BeginChangeCheck();
            selectedUndeadIndex = EditorGUILayout.Popup("Character", selectedUndeadIndex, labels);
            if (EditorGUI.EndChangeCheck())
                selectedPatternIndex = 0;

            return;
        }

        if (zombieOptions.Count == 0)
        {
            EditorGUILayout.HelpBox("No Zombie types found.", MessageType.Warning);
            return;
        }

        string[] zombieLabels = BuildZombieLabels();
        selectedZombieIndex = Mathf.Clamp(selectedZombieIndex, 0, Mathf.Max(0, zombieLabels.Length - 1));

        EditorGUI.BeginChangeCheck();
        selectedZombieIndex = EditorGUILayout.Popup("Character", selectedZombieIndex, zombieLabels);
        if (EditorGUI.EndChangeCheck())
            selectedPatternIndex = 0;
    }

    private void DrawFactionSelector()
    {
        EditorGUILayout.LabelField("Faction");
        EditorGUILayout.BeginHorizontal();

        bool isUndead = selectedFaction == CharacterFactionTab.Undead;
        bool undeadPressed = GUILayout.Toggle(isUndead, "Undead", EditorStyles.miniButtonLeft);
        if (undeadPressed && !isUndead)
        {
            selectedFaction = CharacterFactionTab.Undead;
            selectedPatternIndex = 0;
        }

        bool isZombie = selectedFaction == CharacterFactionTab.Zombie;
        bool zombiePressed = GUILayout.Toggle(isZombie, "Zombie", EditorStyles.miniButtonRight);
        if (zombiePressed && !isZombie)
        {
            selectedFaction = CharacterFactionTab.Zombie;
            selectedPatternIndex = 0;
        }

        EditorGUILayout.EndHorizontal();
    }

    private TeamType GetSelectedTeam()
    {
        return selectedFaction == CharacterFactionTab.Undead
            ? TeamType.Undead
            : TeamType.Enemy;
    }

    private UndeadType GetSelectedUndeadType()
    {
        if (undeadOptions.Count == 0)
            return default(UndeadType);

        selectedUndeadIndex = Mathf.Clamp(selectedUndeadIndex, 0, undeadOptions.Count - 1);
        return undeadOptions[selectedUndeadIndex];
    }

    private EnemyType GetSelectedZombieType()
    {
        if (zombieOptions.Count == 0)
            return default(EnemyType);

        selectedZombieIndex = Mathf.Clamp(selectedZombieIndex, 0, zombieOptions.Count - 1);
        return zombieOptions[selectedZombieIndex];
    }

    private string[] BuildUndeadLabels()
    {
        string[] labels = new string[undeadOptions.Count];
        for (int i = 0; i < undeadOptions.Count; i++)
            labels[i] = undeadOptions[i].ToString();

        return labels;
    }

    private string[] BuildZombieLabels()
    {
        string[] labels = new string[zombieOptions.Count];
        for (int i = 0; i < zombieOptions.Count; i++)
            labels[i] = zombieOptions[i].ToString();

        return labels;
    }

    private void DrawActionSection(UnitAttackPatternProfile profile)
    {
        List<AttackPatternEntry> patterns = profile.Patterns;
        if (patterns.Count == 0)
            patterns.Add(CreateDefaultPattern(profile.Team));

        selectedPatternIndex = Mathf.Clamp(selectedPatternIndex, 0, Mathf.Max(0, patterns.Count - 1));

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);

        bool isEnemyProfile = profile.Team == TeamType.Enemy;

        if (isEnemyProfile)
        {
            selectedPatternIndex = 0;
            EditorGUILayout.LabelField("Action", UnitActionIds.DefaultAction);
        }
        else
        {
            string[] actionLabels = BuildActionLabels(patterns);
            selectedPatternIndex = EditorGUILayout.Popup("Selected Action", selectedPatternIndex, actionLabels);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Action", GUILayout.Width(120f)))
            {
                RecordDatabaseUndo("Add Action Pattern");
                patterns.Add(new AttackPatternEntry { ActionId = GetUniqueActionId(patterns, "Action") });
                selectedPatternIndex = patterns.Count - 1;
                MarkDatabaseDirty();
            }

            EditorGUI.BeginDisabledGroup(patterns.Count <= 1);
            if (GUILayout.Button("Remove Action", GUILayout.Width(120f)))
            {
                RecordDatabaseUndo("Remove Action Pattern");
                patterns.RemoveAt(selectedPatternIndex);
                selectedPatternIndex = Mathf.Clamp(selectedPatternIndex - 1, 0, Mathf.Max(0, patterns.Count - 1));
                MarkDatabaseDirty();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        AttackPatternEntry selectedPattern = patterns[selectedPatternIndex];
        if (selectedPattern == null)
        {
            selectedPattern = CreateDefaultPattern(profile.Team);
            patterns[selectedPatternIndex] = selectedPattern;
        }

        if (isEnemyProfile)
            selectedPattern.ActionId = UnitActionIds.DefaultAction;
        else
        {
            string newActionId = EditorGUILayout.TextField("Action Id", selectedPattern.ActionId);
            if (newActionId != selectedPattern.ActionId)
            {
                RecordDatabaseUndo("Rename Action Id");
                selectedPattern.ActionId = newActionId;
                MarkDatabaseDirty();
            }
        }

        int newMaxRange = EditorGUILayout.IntField("Max Range", selectedPattern.MaxRange);
        newMaxRange = Mathf.Max(0, newMaxRange);
        if (newMaxRange != selectedPattern.MaxRange)
        {
            RecordDatabaseUndo("Change Max Range");
            selectedPattern.MaxRange = newMaxRange;
            TrimOffsetsToRange(selectedPattern);
            MarkDatabaseDirty();
        }

        EditorGUILayout.Space(4f);
        DrawParticleSection(selectedPattern);

        EditorGUILayout.Space(4f);
        DrawGridEditor(selectedPattern);

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill Diamond"))
        {
            RecordDatabaseUndo("Fill Diamond Range");
            FillDiamond(selectedPattern);
            MarkDatabaseDirty();
        }

        if (GUILayout.Button("Clear Cells"))
        {
            RecordDatabaseUndo("Clear Pattern Cells");
            selectedPattern.RelativeTargetOffsets.Clear();
            MarkDatabaseDirty();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawParticleSection(AttackPatternEntry pattern)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Attack Particle", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Particle spawns on target cell only. Use World Offset to fine-tune position.",
            MessageType.None);

        EditorGUI.BeginChangeCheck();

        GameObject newParticlePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Particle Prefab",
            pattern.ParticlePrefab,
            typeof(GameObject),
            false);

        Vector3 newWorldOffset = EditorGUILayout.Vector3Field("World Offset", pattern.ParticleWorldOffset);
        float newSpawnDelay = EditorGUILayout.FloatField("Spawn Delay (sec)", pattern.ParticleSpawnDelay);
        newSpawnDelay = Mathf.Max(0f, newSpawnDelay);

        float newAutoDestroyDelay = EditorGUILayout.FloatField("Auto Destroy (sec)", pattern.ParticleAutoDestroyDelay);
        newAutoDestroyDelay = Mathf.Max(0f, newAutoDestroyDelay);

        if (EditorGUI.EndChangeCheck())
        {
            RecordDatabaseUndo("Change Attack Particle");
            pattern.ParticlePrefab = newParticlePrefab;
            pattern.ParticleWorldOffset = newWorldOffset;
            pattern.ParticleSpawnDelay = newSpawnDelay;
            pattern.ParticleAutoDestroyDelay = newAutoDestroyDelay;
            MarkDatabaseDirty();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawGridEditor(AttackPatternEntry pattern)
    {
        int range = Mathf.Max(0, pattern.MaxRange);
        int visibleRows = Mathf.Min(range * 2 + 1, 13);
        float gridHeight = Mathf.Max(140f, 34f * visibleRows + 40f);

        EditorGUILayout.LabelField("Target Cells", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Center is unit position. Click cells to toggle targetable positions.", MessageType.None);

        HashSet<Vector2Int> offsetSet = new HashSet<Vector2Int>(pattern.RelativeTargetOffsets);

        gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.Height(gridHeight));

        for (int y = range; y >= -range; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(y.ToString(), GUILayout.Width(24f));

            for (int x = -range; x <= range; x++)
            {
                Vector2Int offset = new Vector2Int(x, y);
                bool isCenter = offset == Vector2Int.zero;
                bool selected = offsetSet.Contains(offset);

                Color old = GUI.backgroundColor;
                GUI.backgroundColor = isCenter
                    ? new Color(0.3f, 0.75f, 1f)
                    : selected ? new Color(1f, 0.4f, 0.4f) : new Color(0.85f, 0.85f, 0.85f);

                string label = isCenter ? "U" : (selected ? "X" : "");

                EditorGUI.BeginDisabledGroup(isCenter);
                if (GUILayout.Button(label, GUILayout.Width(30f), GUILayout.Height(30f)))
                {
                    RecordDatabaseUndo("Toggle Pattern Cell");
                    ToggleOffset(pattern, offset);
                    MarkDatabaseDirty();
                    GUI.FocusControl(null);
                }
                EditorGUI.EndDisabledGroup();

                GUI.backgroundColor = old;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(24f);
        for (int x = -range; x <= range; x++)
            GUILayout.Label(x.ToString(), GUILayout.Width(30f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.LabelField("Selected Cells", pattern.RelativeTargetOffsets.Count.ToString());
    }

    private void ToggleOffset(AttackPatternEntry pattern, Vector2Int offset)
    {
        List<Vector2Int> offsets = pattern.RelativeTargetOffsets;
        int index = offsets.IndexOf(offset);
        if (index >= 0)
            offsets.RemoveAt(index);
        else
            offsets.Add(offset);
    }

    private void FillDiamond(AttackPatternEntry pattern)
    {
        List<Vector2Int> offsets = pattern.RelativeTargetOffsets;
        offsets.Clear();

        int range = Mathf.Max(0, pattern.MaxRange);
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > range)
                    continue;

                if (dx == 0 && dy == 0)
                    continue;

                offsets.Add(new Vector2Int(dx, dy));
            }
        }
    }

    private void TrimOffsetsToRange(AttackPatternEntry pattern)
    {
        int range = Mathf.Max(0, pattern.MaxRange);
        pattern.RelativeTargetOffsets.RemoveAll(offset => Mathf.Abs(offset.x) > range || Mathf.Abs(offset.y) > range);
    }

    private string[] BuildActionLabels(List<AttackPatternEntry> patterns)
    {
        string[] labels = new string[patterns.Count];
        for (int i = 0; i < patterns.Count; i++)
        {
            AttackPatternEntry entry = patterns[i];
            string actionId = entry != null ? entry.ActionId : string.Empty;
            labels[i] = string.IsNullOrWhiteSpace(actionId) ? $"Action {i + 1}" : actionId;
        }

        return labels;
    }

    private void BuildCharacterOptions()
    {
        undeadOptions.Clear();
        Array undeadValues = Enum.GetValues(typeof(UndeadType));
        foreach (UndeadType undeadType in undeadValues)
            undeadOptions.Add(undeadType);

        zombieOptions.Clear();
        Array zombieValues = Enum.GetValues(typeof(EnemyType));
        foreach (EnemyType zombieType in zombieValues)
            zombieOptions.Add(zombieType);

        selectedUndeadIndex = Mathf.Clamp(selectedUndeadIndex, 0, Mathf.Max(0, undeadOptions.Count - 1));
        selectedZombieIndex = Mathf.Clamp(selectedZombieIndex, 0, Mathf.Max(0, zombieOptions.Count - 1));
    }

    private UnitAttackPatternProfile GetOrCreateSelectedProfile()
    {
        if (database == null)
            return null;

        TeamType selectedTeam = GetSelectedTeam();
        UndeadType selectedUndeadType = GetSelectedUndeadType();
        EnemyType selectedZombieType = GetSelectedZombieType();

        List<UnitAttackPatternProfile> profiles = database.Profiles;
        for (int i = 0; i < profiles.Count; i++)
        {
            UnitAttackPatternProfile profile = profiles[i];
            if (profile == null)
                continue;

            if (profile.Team != selectedTeam)
                continue;

            if (selectedTeam == TeamType.Undead && profile.UndeadType == selectedUndeadType)
                return profile;

            if (selectedTeam == TeamType.Enemy && profile.EnemyType == selectedZombieType)
                return profile;
        }

        RecordDatabaseUndo("Create Character Pattern Profile");

        UnitAttackPatternProfile created = new UnitAttackPatternProfile
        {
            Team = selectedTeam,
            UndeadType = selectedUndeadType,
            EnemyType = selectedZombieType
        };

        created.Patterns.Add(CreateDefaultPattern(selectedTeam));
        profiles.Add(created);

        MarkDatabaseDirty();
        return created;
    }

    private void NormalizeProfile(UnitAttackPatternProfile profile)
    {
        if (profile == null)
            return;

        List<AttackPatternEntry> patterns = profile.Patterns;
        if (patterns.Count == 0)
            patterns.Add(CreateDefaultPattern(profile.Team));

        if (profile.Team == TeamType.Enemy)
        {
            if (patterns.Count > 1)
                patterns.RemoveRange(1, patterns.Count - 1);

            if (patterns[0] == null)
                patterns[0] = CreateDefaultPattern(profile.Team);

            patterns[0].ActionId = UnitActionIds.DefaultAction;
        }

        selectedPatternIndex = Mathf.Clamp(selectedPatternIndex, 0, Mathf.Max(0, patterns.Count - 1));
    }

    private AttackPatternEntry CreateDefaultPattern(TeamType team)
    {
        return new AttackPatternEntry
        {
            ActionId = UnitActionIds.DefaultAction,
            MaxRange = 1
        };
    }

    private string GetUniqueActionId(List<AttackPatternEntry> patterns, string baseName)
    {
        string root = string.IsNullOrWhiteSpace(baseName) ? "Action" : baseName;
        string candidate = root;
        int suffix = 1;

        while (ContainsActionId(patterns, candidate))
        {
            suffix++;
            candidate = root + suffix;
        }

        return candidate;
    }

    private bool ContainsActionId(List<AttackPatternEntry> patterns, string actionId)
    {
        for (int i = 0; i < patterns.Count; i++)
        {
            AttackPatternEntry entry = patterns[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.ActionId, actionId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void RecordDatabaseUndo(string title)
    {
        if (database != null)
            Undo.RecordObject(database, title);
    }

    private void MarkDatabaseDirty()
    {
        if (database == null)
            return;

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    private void CreateNewDatabase()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Attack Pattern Database",
            "AttackPatternDatabase",
            "asset",
            "Choose path for AttackPatternDatabase asset.");

        if (string.IsNullOrEmpty(path))
            return;

        AttackPatternDatabase asset = ScriptableObject.CreateInstance<AttackPatternDatabase>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        database = asset;
        AttackPatternResolver.SetDatabase(asset);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
#endif
