using System.Collections.Generic;
using UnityEngine;

// 전투 단계의 웨이브(Phase) 진행을 관리한다
// 배치 단계 진입 시 웨이브 인덱스를 리셋해 다음 일차에 재사용 가능하게 한다
// 중간 웨이브 클리어 시 자동으로 다음 웨이브를 시작한다
// 마지막 웨이브 클리어 시에는 GamePhaseController가 배치 단계로 전환한다
public class WaveManager : Singleton<WaveManager>
{
    // 인스펙터에서 웨이브 순서대로 WaveData를 할당한다
    [SerializeField] private List<WaveData> waveDatas = new List<WaveData>();

    // 현재 진행 중인 웨이브 인덱스 (0부터 시작, -1이면 시작 전)
    private int currentWaveIndex = -1;

    // 현재 웨이브에서 살아있는 적 수
    private int aliveEnemyCount;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => waveDatas.Count;
    public bool IsLastWave => currentWaveIndex >= waveDatas.Count - 1;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (e.CurrentPhase == PhaseType.Preparation)
        {
            // 배치 단계 진입 시 리셋해 다음 일차 전투에서 처음부터 다시 사용한다
            ResetForNewDay();
        }
        else if (e.CurrentPhase == PhaseType.Battle)
        {
            StartNextWave();
        }
    }

    // 다음 웨이브를 시작한다
    private void StartNextWave()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waveDatas.Count)
        {
            Debug.LogError("WaveManager: 모든 웨이브가 소진되었는데 다시 시작하려 합니다.");
            return;
        }

        WaveData waveData = waveDatas[currentWaveIndex];
        if (waveData == null)
        {
            Debug.LogError($"WaveData {currentWaveIndex}번이 할당되지 않았습니다. 인스펙터에서 WaveData SO를 연결해주세요.");
            return;
        }

        SpawnEnemies(waveData);

        EventBus.Instance.Publish(new WaveStartedEvent
        {
            WaveNumber = CurrentWaveNumber,
            TotalWaves = TotalWaves
        });
    }

    // 웨이브 데이터를 기반으로 UnitRegistry 풀에서 적을 스폰한다
    private void SpawnEnemies(WaveData waveData)
    {
        aliveEnemyCount = 0;

        foreach (WaveSpawnEntry entry in waveData.SpawnEntries)
        {
            UnitBase spawned = UnitRegistry.Instance.SpawnEnemy(entry.enemyType, entry.spawnPosition);
            if (spawned != null)
                aliveEnemyCount++;
        }
    }

    // 적 유닛 사망 시 살아있는 적 수를 감소시키고 웨이브 클리어를 확인한다
    private void OnEnemyDied(EnemyDiedEvent e)
    {
        if (aliveEnemyCount <= 0) return;

        aliveEnemyCount--;

        if (aliveEnemyCount <= 0)
            HandleWaveCleared();
    }

    // 웨이브 클리어 처리를 수행한다
    private void HandleWaveCleared()
    {
        WaveData waveData = waveDatas[currentWaveIndex];

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.AddWaveReward(waveData.DarkEnergyReward);

        EventBus.Instance.Publish(new WaveClearedEvent { WaveNumber = CurrentWaveNumber });

        if (!IsLastWave)
        {
            // 중간 웨이브 클리어: 자동으로 다음 웨이브를 시작한다
            StartNextWave();
        }
        // 마지막 웨이브 클리어: GamePhaseController가 WaveClearedEvent를 받아 배치 단계로 전환한다
    }

    // 새 일차 시작 전 웨이브 상태를 초기화한다
    private void ResetForNewDay()
    {
        currentWaveIndex = -1;
        aliveEnemyCount = 0;
    }
}
