using System.Collections.Generic;
using UnityEngine;

// 준비 단계에서 적이 등장할 스폰 구역을 그리드에 표시한다
// 플레이어가 다음 웨이브에서 적이 어느 방향에서 오는지 파악할 수 있도록 한다
public class EnemyDirectionIndicator : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Instance.Subscribe<StageChangedEvent>(OnStageChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<StageChangedEvent>(OnStageChanged);
        GridHighlighter.Instance?.ClearEnemySpawnZones();
    }

    private void OnStageChanged(StageChangedEvent e)
    {
        if (e.CurrentStage == StageType.Preparation)
        {
            List<Vector2Int> positions = GridManager.Instance.MapDefinition
                .GetSpawnZonePositions(SpawnZoneType.EnemySpawn);

            GridHighlighter.Instance.ShowEnemySpawnZones(positions);
        }
        else
        {
            GridHighlighter.Instance.ClearEnemySpawnZones();
        }
    }
}
