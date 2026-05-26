using System.Collections.Generic;
using UnityEngine;

// Shows only the enemy spawn zone regions used by the upcoming wave.
public class EnemyDirectionIndicator : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Instance.Subscribe<StageChangedEvent>(OnStageChanged);

        if (GameStageController.Instance != null
            && GameStageController.Instance.CurrentStage == StageType.Preparation)
        {
            ShowUpcomingWaveSpawnZones();
        }
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
            ShowUpcomingWaveSpawnZones();
        }
        else
        {
            GridHighlighter.Instance.ClearEnemySpawnZones();
        }
    }

    private void ShowUpcomingWaveSpawnZones()
    {
        GridHighlighter.Instance.ClearEnemySpawnZones();

        if (GridManager.Instance == null || GridManager.Instance.MapDefinition == null)
            return;

        if (WaveManager.Instance == null || !WaveManager.Instance.TryGetUpcomingWave(out WaveConfig waveConfig))
            return;

        HashSet<Vector2Int> positions = CollectApproachDirectionSpawnZonePositions(
            GridManager.Instance.MapDefinition,
            waveConfig);

        GridHighlighter.Instance.ShowEnemySpawnZones(new List<Vector2Int>(positions));
    }

    private static HashSet<Vector2Int> CollectApproachDirectionSpawnZonePositions(
        MapDefinition map,
        WaveConfig waveConfig)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        if (map == null || waveConfig == null)
            return result;

        for (int i = 0; i < map.Cells.Count; i++)
        {
            MapCellData cell = map.Cells[i];
            Vector2Int position = cell.position;

            if (cell.spawnZone != SpawnZoneType.EnemySpawn)
                continue;

            if (!IsInApproachDirection(map, position, waveConfig.approachDirection))
                continue;

            result.Add(position);
        }

        return result;
    }

    private static bool IsInApproachDirection(
        MapDefinition map,
        Vector2Int position,
        EnemyApproachDirection direction)
    {
        if (direction == EnemyApproachDirection.All)
            return true;

        int westDistance = position.x;
        int eastDistance = map.Width - 1 - position.x;
        int southDistance = position.y;
        int northDistance = map.Height - 1 - position.y;

        switch (direction)
        {
            case EnemyApproachDirection.North:
                return northDistance <= southDistance
                    && northDistance <= westDistance
                    && northDistance <= eastDistance;
            case EnemyApproachDirection.South:
                return southDistance <= northDistance
                    && southDistance <= westDistance
                    && southDistance <= eastDistance;
            case EnemyApproachDirection.East:
                return eastDistance <= westDistance
                    && eastDistance <= northDistance
                    && eastDistance <= southDistance;
            case EnemyApproachDirection.West:
                return westDistance <= eastDistance
                    && westDistance <= northDistance
                    && westDistance <= southDistance;
            default:
                return true;
        }
    }
}
