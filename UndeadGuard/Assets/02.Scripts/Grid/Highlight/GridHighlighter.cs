using System.Collections.Generic;
using UnityEngine;

// 이동 가능 칸, 공격 가능 칸, 경로 미리보기 타일 하이라이트를 관리한다
// 쿼드 오브젝트는 단일 풀에서 꺼내 색상만 바꿔 재사용한다
// 경로 미리보기는 이동 가능 타일 오브젝트의 색상을 직접 변경한다
public class GridHighlighter : Singleton<GridHighlighter>
{
    [SerializeField] private float highlightY = 0.05f;

    [SerializeField] private Color movableColor    = new Color(0.2f, 0.6f, 1f,   0.5f);
    [SerializeField] private Color pathColor       = new Color(1f,   0.8f, 0.2f, 0.65f);
    [SerializeField] private Color attackableColor = new Color(1f,   0.2f, 0.2f, 0.6f);
    [SerializeField] private Color skillRangeColor = new Color(0.8f, 0.2f, 1f,   0.5f);
    [SerializeField] private Color enemySpawnColor = new Color(1f,   0.55f,0.1f, 0.5f);

    // 단일 공유 풀
    private readonly List<GameObject> pool = new List<GameObject>();

    // 이동 가능 타일: 위치 기반 맵으로 관리 (경로 색 변경 시 오브젝트를 바로 찾기 위함)
    private readonly Dictionary<Vector2Int, GameObject> movableMap = new Dictionary<Vector2Int, GameObject>();

    // 현재 경로 색(노란색)으로 표시 중인 위치 목록
    private readonly List<Vector2Int> currentPathPositions = new List<Vector2Int>();

    // 공격/스킬/스폰 타입별 활성 목록
    private readonly List<GameObject> attackableActive = new List<GameObject>();
    private readonly List<GameObject> skillRangeActive = new List<GameObject>();
    private readonly List<GameObject> enemySpawnActive = new List<GameObject>();

    #region Move

    public void ShowMovable(List<Vector2Int> positions)
    {
        ClearMovable();
        foreach (var pos in positions)
        {
            var obj = Rent(movableColor);
            obj.transform.position = GridManager.Instance.GridToWorld(pos) + Vector3.up * highlightY;
            obj.SetActive(true);
            movableMap[pos] = obj;
        }
    }

    public void ClearMovable()
    {
        ClearPath();
        foreach (var obj in movableMap.Values)
        {
            obj.SetActive(false);
            pool.Add(obj);
        }
        movableMap.Clear();
    }

    #endregion

    #region Path Preview

    // 경로 타일의 색상을 노란색으로 바꾼다. 시작 위치(path[0])는 제외한다
    // 이동 가능 타일 오브젝트 자체의 색을 변경하므로 별도 오브젝트를 생성하지 않는다
    public void ShowPath(List<Vector2Int> path)
    {
        ClearPath();
        for (int i = 1; i < path.Count; i++)
        {
            var pos = path[i];
            if (!movableMap.TryGetValue(pos, out var obj)) continue;

            SetColor(obj, pathColor);
            currentPathPositions.Add(pos);
        }
    }

    // 경로 타일을 원래 이동 가능 색(파란색)으로 되돌린다
    public void ClearPath()
    {
        foreach (var pos in currentPathPositions)
        {
            if (movableMap.TryGetValue(pos, out var obj))
                SetColor(obj, movableColor);
        }
        currentPathPositions.Clear();
    }

    #endregion

    #region Attack

    public void ShowAttackable(List<Vector2Int> positions)
    {
        ClearAttackable();
        foreach (var pos in positions)
        {
            var obj = Rent(attackableColor);
            obj.transform.position = GridManager.Instance.GridToWorld(pos) + Vector3.up * highlightY;
            obj.SetActive(true);
            attackableActive.Add(obj);
        }
    }

    public void ClearAttackable()
    {
        Return(attackableActive);
    }

    #endregion

    #region Skill

    public void ShowSkillRange(List<Vector2Int> positions)
    {
        ClearSkillRange();
        foreach (var pos in positions)
        {
            var obj = Rent(skillRangeColor);
            obj.transform.position = GridManager.Instance.GridToWorld(pos) + Vector3.up * highlightY;
            obj.SetActive(true);
            skillRangeActive.Add(obj);
        }
    }

    public void ClearSkillRange()
    {
        Return(skillRangeActive);
    }

    #endregion

    #region Enemy Spawn

    public void ShowEnemySpawnZones(List<Vector2Int> positions)
    {
        ClearEnemySpawnZones();
        foreach (var pos in positions)
        {
            var obj = Rent(enemySpawnColor);
            obj.transform.position = GridManager.Instance.GridToWorld(pos) + Vector3.up * highlightY;
            obj.SetActive(true);
            enemySpawnActive.Add(obj);
        }
    }

    public void ClearEnemySpawnZones()
    {
        Return(enemySpawnActive);
    }

    #endregion

    #region Common

    public void ClearAll()
    {
        ClearMovable();
        ClearAttackable();
        ClearSkillRange();
        ClearEnemySpawnZones();
    }

    // 풀에서 오브젝트를 꺼내고 색상을 설정한다
    private GameObject Rent(Color color)
    {
        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
        }
        else
        {
            obj = CreateHighlightObject();
        }

        SetColor(obj, color);
        return obj;
    }

    // 활성 목록의 오브젝트를 전부 비활성화하고 공유 풀로 반환한다
    private void Return(List<GameObject> active)
    {
        foreach (var obj in active)
        {
            obj.SetActive(false);
            pool.Add(obj);
        }
        active.Clear();
    }

    private void SetColor(GameObject obj, Color color)
    {
        obj.GetComponent<Renderer>().material.color = color;
    }

    // 기본 하이라이트 쿼드 오브젝트를 생성한다
    private GameObject CreateHighlightObject()
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.name = "Highlight";
        obj.transform.SetParent(transform);
        obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        obj.transform.localScale = Vector3.one * (GridManager.TileSpacing * 0.95f);

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f);
        mat.renderQueue = 3000;
        obj.GetComponent<Renderer>().material = mat;

        obj.SetActive(false);
        return obj;
    }

    #endregion
}
