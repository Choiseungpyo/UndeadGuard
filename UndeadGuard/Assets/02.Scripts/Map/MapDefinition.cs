using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapDefinition", menuName = "Map/Map Definition")]
public sealed class MapDefinition : ScriptableObject
{
    [SerializeField] private int size = 10;
    [SerializeField, HideInInspector] private List<MapCellData> cells = new List<MapCellData>();

    public int Size => size;
    public int Width => size;
    public int Height => size;
    public IReadOnlyList<MapCellData> Cells => cells;

    public MapCellData GetCell(int x, int z)
    {
        if (x < 0 || x >= size || z < 0 || z >= size)
        {
            return default;
        }

        int index = GetIndex(x, z);
        if (index < 0 || index >= cells.Count)
        {
            return default;
        }

        return cells[index];
    }

    public bool TryGetCorePosition(out Vector2Int position)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].objectType == StructureType.Core)
            {
                position = cells[i].position;
                return true;
            }
        }

        position = default;
        return false;
    }

    public List<Vector2Int> GetPositions(StructureType type)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].objectType == type)
            {
                result.Add(cells[i].position);
            }
        }

        return result;
    }

    // 특정 스폰 구역에 해당하는 모든 타일 좌표를 반환한다
    public List<Vector2Int> GetSpawnZonePositions(SpawnZoneType zoneType)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].spawnZone == zoneType)
            {
                result.Add(cells[i].position);
            }
        }

        return result;
    }

#if UNITY_EDITOR
    public void Resize(int newSize)
    {
        newSize = Mathf.Max(1, newSize);

        Dictionary<Vector2Int, MapCellData> oldMap = new Dictionary<Vector2Int, MapCellData>();
        for (int i = 0; i < cells.Count; i++)
        {
            oldMap[cells[i].position] = cells[i];
        }

        size = newSize;
        cells = new List<MapCellData>(size * size);

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2Int pos = new Vector2Int(x, z);

                if (oldMap.TryGetValue(pos, out MapCellData oldCell))
                {
                    cells.Add(oldCell);
                }
                else
                {
                    cells.Add(new MapCellData
                    {
                        position = pos,
                        objectType = StructureType.None
                    });
                }
            }
        }
    }

    public void EnsureInitialized()
    {
        if (size <= 0)
        {
            size = 1;
        }

        if (cells == null || cells.Count != size * size)
        {
            Resize(size);
        }
    }

    public void PaintObject(int x, int z, StructureType newType)
    {
        EnsureInitialized();

        if (!IsInside(x, z))
        {
            return;
        }

        if (newType == StructureType.Core)
        {
            ClearSingle(StructureType.Core);
        }

        int index = GetIndex(x, z);
        MapCellData cell = cells[index];
        cell.objectType = newType;
        cells[index] = cell;
    }

    // 지정한 칸의 스폰 구역을 설정한다
    public void PaintSpawnZone(int x, int z, SpawnZoneType zoneType)
    {
        EnsureInitialized();

        if (!IsInside(x, z))
        {
            return;
        }

        int index = GetIndex(x, z);
        MapCellData cell = cells[index];
        cell.spawnZone = zoneType;
        cells[index] = cell;
    }

    public void ClearAllObjects()
    {
        EnsureInitialized();

        for (int i = 0; i < cells.Count; i++)
        {
            MapCellData cell = cells[i];
            cell.objectType = StructureType.None;
            cells[i] = cell;
        }
    }

    // 모든 칸의 스폰 구역을 초기화한다
    public void ClearAllSpawnZones()
    {
        EnsureInitialized();

        for (int i = 0; i < cells.Count; i++)
        {
            MapCellData cell = cells[i];
            cell.spawnZone = SpawnZoneType.None;
            cells[i] = cell;
        }
    }

    private void ClearSingle(StructureType type)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].objectType == type)
            {
                MapCellData cell = cells[i];
                cell.objectType = StructureType.None;
                cells[i] = cell;
            }
        }
    }
#endif

    private bool IsInside(int x, int z)
    {
        return x >= 0 && x < size && z >= 0 && z < size;
    }

    private int GetIndex(int x, int z)
    {
        return z * size + x;
    }
}