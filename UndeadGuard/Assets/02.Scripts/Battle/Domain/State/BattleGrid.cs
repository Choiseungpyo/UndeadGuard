using System;

/// <summary>
/// 전투 맵 전체의 그리드 데이터를 관리하는 클래스.
/// 각 칸의 상태 조회와 맵 범위 판정을 담당하며,
/// Unity 오브젝트와 분리된 순수 전투 데이터로 동작한다.
/// </summary>
public sealed class BattleGrid
{
    private readonly CellData[,] cells;

    public int Width { get; }
    public int Height { get; }

    public BattleGrid(int width, int height)
    {
        Width = width;
        Height = height;
        cells = new CellData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition position = new GridPosition(x, z);
                cells[x, z] = new CellData(position, TileType.Ground, CellObjectType.None);
            }
        }
    }

    public bool IsInside(GridPosition position)
    {
        return position.x >= 0 &&
               position.z >= 0 &&
               position.x < Width &&
               position.z < Height;
    }

    public CellData GetCell(GridPosition position)
    {
        if (!IsInside(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        return cells[position.x, position.z];
    }

    public void SetTileType(GridPosition position, TileType tileType)
    {
        CellData cell = GetCell(position);
        cell.SetTileType(tileType);
    }

    public void SetObjectType(GridPosition position, CellObjectType objectType)
    {
        CellData cell = GetCell(position);
        cell.SetObjectType(objectType);
    }
}