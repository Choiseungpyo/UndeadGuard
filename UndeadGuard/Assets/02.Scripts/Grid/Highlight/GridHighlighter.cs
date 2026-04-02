using System.Collections.Generic;
using UnityEngine;

// 이동 가능 칸, 공격 가능 칸, 경로 미리보기 타일 하이라이트를 관리한다
// 각 하이라이트 종류를 별도 풀로 관리하여 색상을 구분한다
public class GridHighlighter : Singleton<GridHighlighter>
{
    // 이동 가능 칸 색상 (파란색 계열)
    [SerializeField] private Color movableColor = new Color(0.2f, 0.6f, 1f, 0.5f);

    // 경로 미리보기 칸 색상 (노란색 계열)
    [SerializeField] private Color pathColor = new Color(1f, 0.8f, 0.2f, 0.65f);

    // 공격 가능 칸 색상 (빨간색 계열)
    [SerializeField] private Color attackableColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    // 스킬 범위 칸 색상 (보라색 계열)
    [SerializeField] private Color skillRangeColor = new Color(0.8f, 0.2f, 1f, 0.5f);

    // 적 스폰 구역 표시 색상 (주황색 계열)
    [SerializeField] private Color enemySpawnColor = new Color(1f, 0.55f, 0.1f, 0.5f);

    // 이동 가능 칸 풀
    private readonly List<GameObject> movablePool = new List<GameObject>();
    private readonly List<GameObject> movableActive = new List<GameObject>();

    // 경로 미리보기 칸 풀
    private readonly List<GameObject> pathPool = new List<GameObject>();
    private readonly List<GameObject> pathActive = new List<GameObject>();

    // 공격 가능 칸 풀
    private readonly List<GameObject> attackablePool = new List<GameObject>();
    private readonly List<GameObject> attackableActive = new List<GameObject>();

    // 스킬 범위 칸 풀
    private readonly List<GameObject> skillRangePool = new List<GameObject>();
    private readonly List<GameObject> skillRangeActive = new List<GameObject>();

    // 적 스폰 구역 풀
    private readonly List<GameObject> enemySpawnPool = new List<GameObject>();
    private readonly List<GameObject> enemySpawnActive = new List<GameObject>();

    // 지정한 타일 좌표 목록에 이동 가능 하이라이트를 표시한다
    public void ShowMovable(List<Vector2Int> positions)
    {
        ClearMovable();

        foreach (var pos in positions)
        {
            var obj = GetFromPool(movablePool, movableColor);
            obj.transform.position = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.05f;
            obj.SetActive(true);
            movableActive.Add(obj);
        }
    }

    // 지정한 타일 좌표 목록에 공격 가능 하이라이트를 표시한다
    public void ShowAttackable(List<Vector2Int> positions)
    {
        ClearAttackable();

        foreach (var pos in positions)
        {
            var obj = GetFromPool(attackablePool, attackableColor);
            // 이동 가능 칸보다 살짝 위에 표시하여 겹침 시 공격 가능 칸이 위에 보이도록 한다
            obj.transform.position = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.07f;
            obj.SetActive(true);
            attackableActive.Add(obj);
        }
    }

    // 지정한 타일 좌표 목록에 스킬 범위 하이라이트를 표시한다
    public void ShowSkillRange(List<Vector2Int> positions)
    {
        ClearSkillRange();

        foreach (var pos in positions)
        {
            var obj = GetFromPool(skillRangePool, skillRangeColor);
            obj.transform.position = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.09f;
            obj.SetActive(true);
            skillRangeActive.Add(obj);
        }
    }

    // 지정한 경로 타일 목록에 경로 미리보기 하이라이트를 표시한다
    // 시작 위치(path[0])는 제외하고 표시한다
    public void ShowPath(List<Vector2Int> path)
    {
        ClearPath();

        for (int i = 1; i < path.Count; i++)
        {
            var obj = GetFromPool(pathPool, pathColor);
            // 이동 가능 칸보다 살짝 더 위에 표시하여 겹침을 방지한다
            obj.transform.position = GridManager.Instance.GridToWorld(path[i]) + Vector3.up * 0.1f;
            obj.SetActive(true);
            pathActive.Add(obj);
        }
    }

    // 경로 미리보기 하이라이트만 제거한다
    public void ClearPath()
    {
        foreach (var obj in pathActive)
            obj.SetActive(false);

        pathPool.AddRange(pathActive);
        pathActive.Clear();
    }

    // 이동 가능 칸 하이라이트만 제거한다
    public void ClearMovable()
    {
        foreach (var obj in movableActive)
            obj.SetActive(false);

        movablePool.AddRange(movableActive);
        movableActive.Clear();
    }

    // 공격 가능 칸 하이라이트만 제거한다
    public void ClearAttackable()
    {
        foreach (var obj in attackableActive)
            obj.SetActive(false);

        attackablePool.AddRange(attackableActive);
        attackableActive.Clear();
    }

    // 스킬 범위 하이라이트만 제거한다
    public void ClearSkillRange()
    {
        foreach (var obj in skillRangeActive)
            obj.SetActive(false);

        skillRangePool.AddRange(skillRangeActive);
        skillRangeActive.Clear();
    }

    // 지정한 타일 좌표 목록에 적 스폰 구역 하이라이트를 표시한다
    public void ShowEnemySpawnZones(List<Vector2Int> positions)
    {
        ClearEnemySpawnZones();

        foreach (var pos in positions)
        {
            var obj = GetFromPool(enemySpawnPool, enemySpawnColor);
            obj.transform.position = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.03f;
            obj.SetActive(true);
            enemySpawnActive.Add(obj);
        }
    }

    // 적 스폰 구역 하이라이트를 제거한다
    public void ClearEnemySpawnZones()
    {
        foreach (var obj in enemySpawnActive)
            obj.SetActive(false);

        enemySpawnPool.AddRange(enemySpawnActive);
        enemySpawnActive.Clear();
    }

    // 모든 하이라이트를 제거한다
    public void ClearAll()
    {
        ClearMovable();
        ClearPath();
        ClearAttackable();
        ClearSkillRange();
        ClearEnemySpawnZones();
    }

    // 해당 색상 풀에서 오브젝트를 꺼내거나 새로 생성한다
    private GameObject GetFromPool(List<GameObject> pool, Color color)
    {
        if (pool.Count > 0)
        {
            var obj = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            return obj;
        }
        return CreateHighlightObject(color);
    }

    // 지정한 색상의 하이라이트용 쿼드 오브젝트를 생성한다
    private GameObject CreateHighlightObject(Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.name = "Highlight";
        obj.transform.SetParent(transform);
        obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        obj.transform.localScale = Vector3.one * (GridManager.TileSpacing * 0.95f);

        // 콜라이더는 필요 없으므로 제거한다
        Destroy(obj.GetComponent<Collider>());

        var renderer = obj.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.SetFloat("_Surface", 1f);
        mat.renderQueue = 3000;
        renderer.material = mat;

        obj.SetActive(false);
        return obj;
    }
}
