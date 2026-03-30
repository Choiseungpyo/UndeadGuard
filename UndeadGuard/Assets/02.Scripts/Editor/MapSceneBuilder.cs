#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MapSceneBuilder
{
    private const string RootName = "Map";
    private const float TileSpacing = 4f;

    public static void Build(MapDefinition definition, MapVisualSet visualSet)
    {
        if (definition == null)
        {
            Debug.LogError("MapDefinition is null.");
            return;
        }

        if (visualSet == null)
        {
            Debug.LogError("MapVisualSet is null.");
            return;
        }

        Clear();

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Map Preview");

        GameObject groundRoot = new GameObject("Ground");
        GameObject objectRoot = new GameObject("Objects");

        groundRoot.transform.SetParent(root.transform, false);
        objectRoot.transform.SetParent(root.transform, false);

        for (int i = 0; i < definition.Cells.Count; i++)
        {
            MapCellData cell = definition.Cells[i];

            BuildGround(visualSet, groundRoot.transform, cell);
            BuildObject(visualSet, objectRoot.transform, cell);
        }

        Selection.activeGameObject = root;
    }

    public static void Clear()
    {
        GameObject root = GameObject.Find(RootName);
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void BuildGround(MapVisualSet visualSet, Transform parent, MapCellData cell)
    {
        CreatePlacedInstance(
            visualSet.GroundPrefab,
            $"Ground_{cell.position.x}_{cell.position.y}",
            parent,
            GetCellCenter(cell.position));
    }

    private static void BuildObject(MapVisualSet visualSet, Transform parent, MapCellData cell)
    {
        GameObject prefab = null;

        switch (cell.objectType)
        {
            case StructureType.Wall:
                prefab = visualSet.WallPrefab;
                break;

            case StructureType.Core:
                prefab = visualSet.CorePrefab;
                break;

            case StructureType.RevivalAltar:
                prefab = visualSet.RevivalAltarPrefab;
                break;
        }

        if (prefab == null)
        {
            return;
        }

        CreatePlacedInstance(
            prefab,
            $"{cell.objectType}_{cell.position.x}_{cell.position.y}",
            parent,
            GetCellCenter(cell.position));
    }

    private static GameObject CreatePlacedInstance(
        GameObject prefab,
        string objectName,
        Transform parent,
        Vector3 worldCenter)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.name = objectName;
        instance.transform.SetParent(parent, false);
        instance.transform.position = worldCenter;
        instance.transform.rotation = Quaternion.identity;

        return instance;
    }

    private static Vector3 GetCellCenter(Vector2Int cell)
    {
        return new Vector3(
            (cell.x + 0.5f) * TileSpacing,
            0f,
            (cell.y + 0.5f) * TileSpacing);
    }
}
#endif