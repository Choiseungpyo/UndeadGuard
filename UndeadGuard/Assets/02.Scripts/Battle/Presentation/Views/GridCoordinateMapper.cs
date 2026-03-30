using UnityEngine;

/// <summary>
/// 그리드 좌표와 월드 좌표를 변환하는 보조 클래스.
/// 현재 맵은 셀 간격 4를 기준으로 동작한다.
/// </summary>
public sealed class GridCoordinateMapper : MonoBehaviour
{
    private const float CellSpacing = 4f;

    public Vector3 GetWorldPosition(GridPosition gridPosition)
    {
        return new Vector3(
            (gridPosition.x + 0.5f) * CellSpacing,
            0f,
            (gridPosition.z + 0.5f) * CellSpacing);
    }

    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / CellSpacing);
        int z = Mathf.FloorToInt(worldPosition.z / CellSpacing);

        return new GridPosition(x, z);
    }
}