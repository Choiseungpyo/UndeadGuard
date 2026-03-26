using UnityEngine;

/// <summary>
/// 월드 좌표와 그리드 좌표를 변환하는 뷰 보조 클래스.
/// 전투 로직의 논리 좌표를 실제 Unity 월드 위치와 연결한다.
/// </summary>
public sealed class GridCoordinateMapper : MonoBehaviour
{
    [SerializeField] private Vector3 origin = Vector3.zero;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float worldY = 0f;

    public Vector3 GetWorldPosition(GridPosition gridPosition)
    {
        return origin + new Vector3(
            (gridPosition.x + 0.5f) * cellSize,
            worldY,
            (gridPosition.z + 0.5f) * cellSize);
    }

    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition - origin;

        int x = Mathf.FloorToInt(localPosition.x / cellSize);
        int z = Mathf.FloorToInt(localPosition.z / cellSize);

        return new GridPosition(x, z);
    }
}