#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class MapEditorWindow : EditorWindow
{
    private MapDefinition mapDefinition;
    private MapVisualSet mapVisualSet;

    private int resizeN = 8;

    // 두 브러시 탭은 완전히 독립적이다. 각자 선택 상태를 유지한다
    private enum BrushTab { SpawnZone, MapObject }
    private BrushTab activeBrushTab = BrushTab.SpawnZone;

    private SpawnZoneType selectedSpawnBrush = SpawnZoneType.PlayerSpawn;
    private StructureType selectedObjectBrush = StructureType.Wall;

    private Vector2 windowScroll;
    private Vector2 gridScroll;

    [MenuItem("Tools/Map/Map Editor")]
    public static void Open()
    {
        GetWindow<MapEditorWindow>("Map Editor");
    }

    private void OnGUI()
    {
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);

        DrawHeader();
        EditorGUILayout.Space(8f);

        if (mapDefinition == null)
        {
            EditorGUILayout.HelpBox("MapDefinition asset을 선택하거나 새로 만드세요.", MessageType.Info);

            if (GUILayout.Button("Create New MapDefinition"))
                CreateNewMapAsset();

            EditorGUILayout.EndScrollView();
            return;
        }

        mapDefinition.EnsureInitialized();

        DrawMapSettings();
        EditorGUILayout.Space(8f);

        DrawBrushTabs();
        EditorGUILayout.Space(8f);

        DrawGrid();
        EditorGUILayout.Space(8f);

        DrawPreviewButtons();

        if (GUI.changed)
            EditorUtility.SetDirty(mapDefinition);

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Map Definition", EditorStyles.boldLabel);

        mapDefinition = (MapDefinition)EditorGUILayout.ObjectField(
            "Map Asset", mapDefinition, typeof(MapDefinition), false);

        mapVisualSet = (MapVisualSet)EditorGUILayout.ObjectField(
            "Map Visual Set", mapVisualSet, typeof(MapVisualSet), false);
    }

    private void DrawMapSettings()
    {
        EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);

        int currentSize = mapDefinition.Size;
        resizeN = EditorGUILayout.IntField("N", currentSize);
        resizeN = Mathf.Max(1, resizeN);

        if (resizeN != currentSize)
        {
            Undo.RecordObject(mapDefinition, "Resize Map");
            mapDefinition.Resize(resizeN);
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
        }
    }

    // 탭 버튼으로 두 브러시 모드를 전환한다. 각 탭은 독립적인 선택 상태를 유지한다
    private void DrawBrushTabs()
    {
        EditorGUILayout.BeginHorizontal();

        DrawTabButton(BrushTab.SpawnZone, "Spawn Zone Brush");
        DrawTabButton(BrushTab.MapObject, "Map Object Brush");

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);

        if (activeBrushTab == BrushTab.SpawnZone)
            DrawSpawnZonePalette();
        else
            DrawObjectPalette();
    }

    private void DrawTabButton(BrushTab tab, string label)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = activeBrushTab == tab ? new Color(0.5f, 0.8f, 1f) : new Color(0.75f, 0.75f, 0.75f);

        if (GUILayout.Button(label, GUILayout.Height(30f)))
        {
            activeBrushTab = tab;
            GUI.FocusControl(null);
        }

        GUI.backgroundColor = old;
    }

    private void DrawSpawnZonePalette()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Spawn Zone Brush", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        // Erase: 해당 칸의 스폰 존을 지운다 (SpawnZoneType.None)
        DrawSpawnZoneBrushButton(SpawnZoneType.None, "Erase", new Color(0.85f, 0.85f, 0.85f));
        DrawSpawnZoneBrushButton(SpawnZoneType.PlayerSpawn, "Player", new Color(0.30f, 0.55f, 1.00f));
        DrawSpawnZoneBrushButton(SpawnZoneType.EnemySpawn, "Enemy", new Color(1.00f, 0.35f, 0.35f));
        EditorGUILayout.EndHorizontal();

        string spawnLabel = selectedSpawnBrush == SpawnZoneType.None ? "Erase" : selectedSpawnBrush.ToString();
        EditorGUILayout.LabelField("Selected", spawnLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear All Spawn Zones", GUILayout.Width(160f)))
        {
            Undo.RecordObject(mapDefinition, "Clear All Spawn Zones");
            mapDefinition.ClearAllSpawnZones();
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawObjectPalette()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Map Object Brush", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Spawn Zone이 있는 칸에는 오브젝트를 배치할 수 없습니다.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        // None: 해당 칸의 구조물을 지운다 (StructureType.None)
        DrawObjectBrushButton(StructureType.None, "None", new Color(0.85f, 0.85f, 0.85f), mapVisualSet != null ? mapVisualSet.NoneIcon : null);
        DrawObjectBrushButton(StructureType.Wall, "Wall", new Color(0.25f, 0.25f, 0.25f), mapVisualSet != null ? mapVisualSet.WallIcon : null);
        DrawObjectBrushButton(StructureType.Core, "Core", new Color(0.20f, 0.90f, 0.20f), mapVisualSet != null ? mapVisualSet.CoreIcon : null);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Selected", selectedObjectBrush.ToString());

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear All Objects", GUILayout.Width(130f)))
        {
            Undo.RecordObject(mapDefinition, "Clear All Objects");
            mapDefinition.ClearAllObjects();
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawObjectBrushButton(StructureType brush, string fallbackText, Color color, Texture2D icon)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = selectedObjectBrush == brush ? color * 0.7f : color;

        GUIContent content = icon != null
            ? new GUIContent(icon, brush.ToString())
            : new GUIContent(fallbackText);

        if (GUILayout.Button(content, GUILayout.Width(64f), GUILayout.Height(64f)))
        {
            selectedObjectBrush = brush;
            GUI.FocusControl(null);
        }

        GUI.backgroundColor = old;
    }

    private void DrawSpawnZoneBrushButton(SpawnZoneType zone, string label, Color color)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = selectedSpawnBrush == zone ? color * 0.7f : color;

        if (GUILayout.Button(label, GUILayout.Width(64f), GUILayout.Height(64f)))
        {
            selectedSpawnBrush = zone;
            GUI.FocusControl(null);
        }

        GUI.backgroundColor = old;
    }

    private void DrawGrid()
    {
        EditorGUILayout.LabelField("Map Grid", EditorStyles.boldLabel);

        int visibleRows = Mathf.Min(mapDefinition.Size, 10);
        float gridHeight = Mathf.Max(140f, 36f * visibleRows + 30f);

        gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.Height(gridHeight));

        Event e = Event.current;
        bool isMouseEvent = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0;
        bool didPaint = false;

        GUIStyle cellStyle = GUI.skin.button;

        for (int z = mapDefinition.Size - 1; z >= 0; z--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(z.ToString(), GUILayout.Width(28f));

            for (int x = 0; x < mapDefinition.Size; x++)
            {
                MapCellData cell = mapDefinition.GetCell(x, z);

                Rect cellRect = GUILayoutUtility.GetRect(
                    new GUIContent(GetCellText(cell)), cellStyle,
                    GUILayout.Width(34f), GUILayout.Height(34f));

                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = GetCellColor(cell);
                GUI.Box(cellRect, GetCellText(cell), cellStyle);
                GUI.backgroundColor = oldBg;

                if (isMouseEvent && cellRect.Contains(e.mousePosition))
                {
                    PaintCell(x, z);
                    didPaint = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(28f);
        for (int x = 0; x < mapDefinition.Size; x++)
            GUILayout.Label(x.ToString(), GUILayout.Width(34f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        if (didPaint)
        {
            e.Use();
            EditorUtility.SetDirty(mapDefinition);
            Repaint();
        }
    }

    // 현재 활성 탭과 브러시 선택에 따라 셀을 칠한다
    // Map Object 브러시는 SpawnZone이 없는 칸에만 적용된다
    private void PaintCell(int x, int z)
    {
        Undo.RecordObject(mapDefinition, "Paint Map Cell");

        if (activeBrushTab == BrushTab.SpawnZone)
        {
            mapDefinition.PaintSpawnZone(x, z, selectedSpawnBrush);
        }
        else
        {
            // 스폰 존이 지정된 칸에는 오브젝트를 배치하지 않는다
            MapCellData cell = mapDefinition.GetCell(x, z);
            if (cell.spawnZone != SpawnZoneType.None)
                return;

            mapDefinition.PaintObject(x, z, selectedObjectBrush);
        }
    }

    // 미리보기 버튼은 그리드 아래에 위치한다
    private void DrawPreviewButtons()
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        if (mapVisualSet == null)
            EditorGUILayout.HelpBox("3D 미리보기를 위해서 Map Visual Set을 할당하세요.", MessageType.Warning);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(mapDefinition == null || mapVisualSet == null);
        if (GUILayout.Button("Build 3D Preview", GUILayout.Height(28f)))
        {
            AssetDatabase.SaveAssets();
            MapSceneBuilder.Build(mapDefinition, mapVisualSet);
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Clear 3D Preview", GUILayout.Height(28f)))
        {
            MapSceneBuilder.Clear();

            // 3D 프리뷰를 초기화하면 맵 그리드 데이터도 함께 초기화한다
            if (mapDefinition != null)
            {
                Undo.RecordObject(mapDefinition, "Clear 3D Preview");
                mapDefinition.ClearAllObjects();
                EditorUtility.SetDirty(mapDefinition);
                AssetDatabase.SaveAssets();
                Repaint();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private Color GetCellColor(MapCellData cell)
    {
        switch (cell.objectType)
        {
            case StructureType.Wall:
                return new Color(0.25f, 0.25f, 0.25f);

            case StructureType.Core:
                return new Color(0.20f, 0.90f, 0.20f);
        }

        switch (cell.spawnZone)
        {
            case SpawnZoneType.PlayerSpawn:
                return new Color(0.45f, 0.65f, 1.00f);

            case SpawnZoneType.EnemySpawn:
                return new Color(1.00f, 0.50f, 0.50f);

            default:
                return new Color(0.75f, 0.75f, 0.75f);
        }
    }

    private string GetCellText(MapCellData cell)
    {
        switch (cell.objectType)
        {
            case StructureType.Wall:
                return "W";

            case StructureType.Core:
                return "C";
        }

        switch (cell.spawnZone)
        {
            case SpawnZoneType.PlayerSpawn:
                return "P";

            case SpawnZoneType.EnemySpawn:
                return "E";

            default:
                return "";
        }
    }

    private void CreateNewMapAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Map Definition",
            "MapDefinition",
            "asset",
            "Choose a path for the new asset.");

        if (string.IsNullOrEmpty(path))
            return;

        MapDefinition asset = ScriptableObject.CreateInstance<MapDefinition>();
        asset.Resize(8);

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        mapDefinition = asset;
        resizeN = asset.Size;

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
#endif
