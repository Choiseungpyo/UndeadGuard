using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이동 가능 칸 마커와 이동 경로 마커를 화면에 표시하는 뷰 클래스.
/// 전투 로직 결과를 플레이어가 시각적으로 이해할 수 있게 보여준다.
/// </summary>
public sealed class GridView : MonoBehaviour
{
    [SerializeField] private GridCoordinateMapper coordinateMapper;
    [SerializeField] private Transform markerRoot;
    [SerializeField] private GameObject moveMarkerPrefab;
    [SerializeField] private GameObject pathMarkerPrefab;

    [SerializeField] private float moveMarkerYOffset = 0.01f;
    [SerializeField] private float pathMarkerYOffset = 0.05f;

    private readonly List<GameObject> moveMarkers = new List<GameObject>();
    private readonly List<GameObject> pathMarkers = new List<GameObject>();

    public void ShowMoveRange(IEnumerable<GridPosition> positions)
    {
        ClearMoveRange();

        if (moveMarkerPrefab == null || markerRoot == null)
        {
            return;
        }

        foreach (GridPosition position in positions)
        {
            GameObject marker = CreateMarker(moveMarkerPrefab, position, moveMarkerYOffset);
            if (marker != null)
            {
                moveMarkers.Add(marker);
            }
        }
    }

    public void ClearMoveRange()
    {
        for (int i = 0; i < moveMarkers.Count; i++)
        {
            if (moveMarkers[i] != null)
            {
                Destroy(moveMarkers[i]);
            }
        }

        moveMarkers.Clear();
    }

    public void ShowPathPreview(IReadOnlyList<GridPosition> path)
    {
        ClearPathPreview();

        if (pathMarkerPrefab == null || markerRoot == null || path == null)
        {
            return;
        }

        for (int i = 1; i < path.Count; i++)
        {
            GameObject marker = CreateMarker(pathMarkerPrefab, path[i], pathMarkerYOffset);
            if (marker != null)
            {
                pathMarkers.Add(marker);
            }
        }
    }

    public void ClearPathPreview()
    {
        for (int i = 0; i < pathMarkers.Count; i++)
        {
            if (pathMarkers[i] != null)
            {
                Destroy(pathMarkers[i]);
            }
        }

        pathMarkers.Clear();
    }

    private GameObject CreateMarker(GameObject prefab, GridPosition position, float yOffset)
    {
        if (prefab == null || markerRoot == null)
        {
            return null;
        }

        Vector3 worldPosition = coordinateMapper.GetWorldPosition(position);
        worldPosition.y += yOffset;

        GameObject marker = Instantiate(prefab, markerRoot);
        marker.transform.SetPositionAndRotation(worldPosition, prefab.transform.rotation);

        return marker;
    }
}