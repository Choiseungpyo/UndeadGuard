#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class MapEditorWindow : EditorWindow
{
    private MapDefinition mapDefinition;
    private MapVisualSet mapVisualSet;

    private int resizeN = 8;
    private StructureType selectedBrush = StructureType.Wall;

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
            EditorGUILayout.HelpBox("MapDefinition asset을 선택하거나 새로 만들어.", MessageType.Info);

            if (GUILayout.Button("Create New MapDefinition"))
            {
                CreateNewMapAsset();
            }

            EditorGUILayout.EndScrollView();
            return;
        }

        mapDefinition.EnsureInitialized();

        DrawMapSettings();
        EditorGUILayout.Space(8f);

        DrawBrushPalette();
        EditorGUILayout.Space(8f);

        DrawPreviewButtons();
        EditorGUILayout.Space(8f);

        DrawGrid();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(mapDefinition);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Map Definition", EditorStyles.boldLabel);

        mapDefinition = (MapDefinition)EditorGUILayout.ObjectField(
            "Map Asset",
            mapDefinition,
            typeof(MapDefinition),
            false);

        mapVisualSet = (MapVisualSet)EditorGUILayout.ObjectField(
            "Map Visual Set",
            mapVisualSet,
            typeof(MapVisualSet),
            false);
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

        if (GUILayout.Button("Clear All Objects"))
        {
            Undo.RecordObject(mapDefinition, "Clear All Objects");
            mapDefinition.ClearAllObjects();
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
            Repaint();
        }
    }

    private void DrawBrushPalette()
    {
        EditorGUILayout.LabelField("Map Object Brush", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawBrushButton(StructureType.None, "None", new Color(0.85f, 0.85f, 0.85f), mapVisualSet != null ? mapVisualSet.NoneIcon : null);
        DrawBrushButton(StructureType.Wall, "Wall", new Color(0.25f, 0.25f, 0.25f), mapVisualSet != null ? mapVisualSet.WallIcon : null);
        DrawBrushButton(StructureType.Core, "Core", new Color(0.20f, 0.90f, 0.20f), mapVisualSet != null ? mapVisualSet.CoreIcon : null);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Selected", selectedBrush.ToString());
    }

    private void DrawBrushButton(StructureType brush, string fallbackText, Color color, Texture2D icon)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = selectedBrush == brush ? color * 0.85f : color;

        GUIContent content = icon != null
            ? new GUIContent(icon, brush.ToString())
            : new GUIContent(fallbackText, brush.ToString());

        if (GUILayout.Button(content, GUILayout.Width(64f), GUILayout.Height(64f)))
        {
            selectedBrush = brush;
            GUI.FocusControl(null);
        }

        GUI.backgroundColor = old;
    }

    private void DrawPreviewButtons()
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        if (mapVisualSet == null)
        {
            EditorGUILayout.HelpBox("3D 프리뷰를 만들려면 Map Visual Set을 연결해.", MessageType.Warning);
        }

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
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid()
    {
        EditorGUILayout.LabelField("Map Grid", EditorStyles.boldLabel);

        int visibleRows = Mathf.Min(mapDefinition.Size, 10);
        float gridHeight = Mathf.Max(140f, 36f * visibleRows + 30f);

        gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.Height(gridHeight));

        for (int z = mapDefinition.Size - 1; z >= 0; z--)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(z.ToString(), GUILayout.Width(28f));

            for (int x = 0; x < mapDefinition.Size; x++)
            {
                MapCellData cell = mapDefinition.GetCell(x, z);

                Color old = GUI.backgroundColor;
                GUI.backgroundColor = GetCellColor(cell);

                if (GUILayout.Button(GetCellText(cell), GUILayout.Width(34f), GUILayout.Height(34f)))
                {
                    Undo.RecordObject(mapDefinition, "Paint Map Cell");
                    mapDefinition.PaintObject(x, z, selectedBrush);
                    EditorUtility.SetDirty(mapDefinition);
                }

                GUI.backgroundColor = old;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(28f);

        for (int x = 0; x < mapDefinition.Size; x++)
        {
            GUILayout.Label(x.ToString(), GUILayout.Width(34f));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private Color GetCellColor(MapCellData cell)
    {
        switch (cell.objectType)
        {
            case StructureType.Wall:
                return new Color(0.25f, 0.25f, 0.25f);

            case StructureType.Core:
                return new Color(0.20f, 0.90f, 0.20f);

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
        {
            return;
        }

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