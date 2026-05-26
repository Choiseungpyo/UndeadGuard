#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// WaveSet ScriptableObject를 시각적으로 편집하는 에디터 창
// EnemySpawn 구역을 맵 그리드에 표시하고 웨이브별로 적 배치 위치를 설정한다
public sealed class WaveEditorWindow : EditorWindow
{
    private WaveSet waveSet;
    private MapDefinition mapDefinition;

    // 현재 선택된 웨이브 탭 인덱스
    private int selectedWaveIndex;

    // 현재 선택된 적 브러시
    private EnemyType selectedBrush = EnemyType.Zombie;

    // Erase 브러시 활성 여부
    private bool isEraseBrush;

    private Vector2 windowScroll;
    private Vector2 gridScroll;

    [MenuItem("Tools/Wave/Wave Editor")]
    public static void Open()
    {
        GetWindow<WaveEditorWindow>("Wave Editor");
    }

    private void OnGUI()
    {
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);

        DrawHeader();
        EditorGUILayout.Space(8f);

        if (waveSet == null)
        {
            EditorGUILayout.HelpBox("WaveSet asset을 선택하거나 새로 만드세요.", MessageType.Info);

            if (GUILayout.Button("Create New WaveSet"))
                CreateNewWaveSet();

            EditorGUILayout.EndScrollView();
            return;
        }

        if (mapDefinition == null)
        {
            EditorGUILayout.HelpBox("MapDefinition을 할당해야 그리드를 표시할 수 있습니다.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawWaveTabs();
        EditorGUILayout.Space(8f);

        if (waveSet.WaveCount == 0)
        {
            EditorGUILayout.HelpBox("+ 버튼으로 웨이브를 추가하세요.", MessageType.Info);
        }
        else
        {
            if (selectedWaveIndex >= waveSet.WaveCount)
                selectedWaveIndex = waveSet.WaveCount - 1;

            DrawWavePanel(waveSet.WaveList[selectedWaveIndex]);
        }

        if (GUI.changed)
            EditorUtility.SetDirty(waveSet);

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Wave Editor", EditorStyles.boldLabel);

        waveSet = (WaveSet)EditorGUILayout.ObjectField(
            "Wave Set", waveSet, typeof(WaveSet), false);

        mapDefinition = (MapDefinition)EditorGUILayout.ObjectField(
            "Map Definition", mapDefinition, typeof(MapDefinition), false);
    }

    // 웨이브 탭 버튼 + 추가/제거 버튼을 그린다
    private void DrawWaveTabs()
    {
        EditorGUILayout.BeginHorizontal();

        for (int i = 0; i < waveSet.WaveCount; i++)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = selectedWaveIndex == i
                ? new Color(0.5f, 0.8f, 1f)
                : new Color(0.75f, 0.75f, 0.75f);

            if (GUILayout.Button($"Wave {i + 1}", GUILayout.Height(28f)))
            {
                selectedWaveIndex = i;
                GUI.FocusControl(null);
            }

            GUI.backgroundColor = old;
        }

        Color addOld = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("+", GUILayout.Width(32f), GUILayout.Height(28f)))
        {
            Undo.RecordObject(waveSet, "Add Wave");
            waveSet.AddWave();
            selectedWaveIndex = waveSet.WaveCount - 1;
            EditorUtility.SetDirty(waveSet);
            AssetDatabase.SaveAssets();
        }
        GUI.backgroundColor = addOld;

        EditorGUI.BeginDisabledGroup(waveSet.WaveCount == 0);
        Color removeOld = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("-", GUILayout.Width(32f), GUILayout.Height(28f)))
        {
            Undo.RecordObject(waveSet, "Remove Wave");
            waveSet.RemoveWave(selectedWaveIndex);
            if (selectedWaveIndex >= waveSet.WaveCount && selectedWaveIndex > 0)
                selectedWaveIndex--;
            EditorUtility.SetDirty(waveSet);
            AssetDatabase.SaveAssets();
        }
        GUI.backgroundColor = removeOld;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
    }

    // 웨이브 보상 설정 + 브러시 팔레트 + 그리드를 그린다
    private void DrawWavePanel(WaveConfig wave)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Wave {selectedWaveIndex + 1} 설정", EditorStyles.boldLabel);

        int newReward = EditorGUILayout.IntField("웨이브 클리어 보너스 (다크에너지)", wave.darkEnergyReward);
        if (newReward != wave.darkEnergyReward)
        {
            Undo.RecordObject(waveSet, "Change Reward");
            wave.darkEnergyReward = Mathf.Max(0, newReward);
        }

        EnemyApproachDirection newDirection = (EnemyApproachDirection)EditorGUILayout.EnumPopup(
            "Enemy Approach Direction",
            wave.approachDirection);

        if (newDirection != wave.approachDirection)
        {
            Undo.RecordObject(waveSet, "Change Approach Direction");
            wave.approachDirection = newDirection;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4f);

        DrawBrushPalette();
        EditorGUILayout.Space(4f);

        DrawGrid(wave);
    }

    // 적 종류 선택 브러시 팔레트를 그린다
    private void DrawBrushPalette()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Enemy Brush", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Erase 브러시
        Color eraseOld = GUI.backgroundColor;
        GUI.backgroundColor = isEraseBrush
            ? new Color(0.6f, 0.6f, 0.6f) * 0.7f
            : new Color(0.85f, 0.85f, 0.85f);

        if (GUILayout.Button("Erase", GUILayout.Width(64f), GUILayout.Height(64f)))
        {
            isEraseBrush = true;
            GUI.FocusControl(null);
        }
        GUI.backgroundColor = eraseOld;

        // EnemyType 버튼 (enum 값 순서대로)
        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
        {
            Color brushColor = GetEnemyColor(type);
            Color brushOld = GUI.backgroundColor;
            GUI.backgroundColor = (!isEraseBrush && selectedBrush == type)
                ? brushColor * 0.7f
                : brushColor;

            if (GUILayout.Button(type.ToString(), GUILayout.Width(64f), GUILayout.Height(64f)))
            {
                selectedBrush = type;
                isEraseBrush = false;
                GUI.FocusControl(null);
            }

            GUI.backgroundColor = brushOld;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    // 맵 그리드를 그린다. EnemySpawn 칸만 클릭 가능하다
    private void DrawGrid(WaveConfig wave)
    {
        EditorGUILayout.LabelField("Spawn Grid", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("빨간 칸(E)은 EnemySpawn 구역입니다. 클릭하면 적을 배치하거나 지울 수 있습니다.", MessageType.None);

        mapDefinition.EnsureInitialized();
        int size = mapDefinition.Size;

        // 현재 웨이브의 배치 정보를 Dictionary로 변환한다
        var spawnMap = BuildSpawnMap(wave);

        int visibleRows = Mathf.Min(size, 12);
        float gridHeight = Mathf.Max(140f, 36f * visibleRows + 30f);

        gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.Height(gridHeight));

        Event e = Event.current;
        bool isMouseEvent = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0;
        bool didPaint = false;

        GUIStyle cellStyle = GUI.skin.button;

        for (int z = size - 1; z >= 0; z--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(z.ToString(), GUILayout.Width(28f));

            for (int x = 0; x < size; x++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                MapCellData cell = mapDefinition.GetCell(x, z);
                bool isEnemySpawn = cell.spawnZone == SpawnZoneType.EnemySpawn;

                GetCellDisplay(cell, pos, isEnemySpawn, spawnMap,
                    out string cellText, out Color cellColor);

                Rect cellRect = GUILayoutUtility.GetRect(
                    new GUIContent(cellText), cellStyle,
                    GUILayout.Width(34f), GUILayout.Height(34f));

                EditorGUI.BeginDisabledGroup(!isEnemySpawn);
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = cellColor;
                GUI.Box(cellRect, cellText, cellStyle);
                GUI.backgroundColor = oldBg;
                EditorGUI.EndDisabledGroup();

                if (isEnemySpawn && isMouseEvent && cellRect.Contains(e.mousePosition))
                {
                    PaintCell(wave, pos);
                    didPaint = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // x축 좌표 라벨
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(28f);
        for (int x = 0; x < size; x++)
            GUILayout.Label(x.ToString(), GUILayout.Width(34f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        if (didPaint)
        {
            e.Use();
            EditorUtility.SetDirty(waveSet);
            AssetDatabase.SaveAssets();
            Repaint();
        }
    }

    // 클릭한 EnemySpawn 칸에 현재 브러시를 적용한다
    private void PaintCell(WaveConfig wave, Vector2Int pos)
    {
        Undo.RecordObject(waveSet, "Paint Wave Cell");

        if (isEraseBrush)
        {
            wave.spawnEntries.RemoveAll(entry => entry.spawnPosition == pos);
        }
        else
        {
            int idx = wave.spawnEntries.FindIndex(entry => entry.spawnPosition == pos);
            var newEntry = new WaveSpawnEntry { enemyType = selectedBrush, spawnPosition = pos };

            if (idx >= 0)
                wave.spawnEntries[idx] = newEntry;
            else
                wave.spawnEntries.Add(newEntry);
        }
    }

    private Dictionary<Vector2Int, EnemyType> BuildSpawnMap(WaveConfig wave)
    {
        var map = new Dictionary<Vector2Int, EnemyType>();
        foreach (var entry in wave.spawnEntries)
            map[entry.spawnPosition] = entry.enemyType;
        return map;
    }

    private void GetCellDisplay(MapCellData cell, Vector2Int pos, bool isEnemySpawn,
        Dictionary<Vector2Int, EnemyType> spawnMap, out string text, out Color color)
    {
        if (isEnemySpawn)
        {
            if (spawnMap.TryGetValue(pos, out EnemyType placed))
            {
                text = GetEnemyLabel(placed);
                color = GetEnemyColor(placed);
            }
            else
            {
                text = "E";
                color = new Color(1.0f, 0.5f, 0.5f);
            }
            return;
        }

        // 비-스폰 칸: 구조물 표시
        color = new Color(0.6f, 0.6f, 0.6f);
        switch (cell.objectType)
        {
            case StructureType.Wall:  text = "W"; return;
            case StructureType.Core:  text = "C"; return;
        }

        switch (cell.spawnZone)
        {
            case SpawnZoneType.PlayerSpawn: text = "P"; color = new Color(0.45f, 0.65f, 1.0f); return;
        }

        text = "";
    }

    private Color GetEnemyColor(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Zombie: return new Color(0.8f, 0.4f, 0.1f);
            default:               return new Color(0.7f, 0.3f, 0.7f);
        }
    }

    private string GetEnemyLabel(EnemyType type)
    {
        string name = type.ToString();
        return name.Length > 0 ? name.Substring(0, 1).ToUpper() : "?";
    }

    private void CreateNewWaveSet()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Wave Set", "WaveSet", "asset", "저장 위치를 선택하세요.");

        if (string.IsNullOrEmpty(path)) return;

        WaveSet asset = ScriptableObject.CreateInstance<WaveSet>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        waveSet = asset;
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
#endif
