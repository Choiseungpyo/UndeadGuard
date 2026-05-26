using System.Collections.Generic;
using UnityEngine;

// 전투 단계의 웨이브(Phase) 진행을 관리한다
// 배치 단계 진입 시 웨이브 인덱스를 리셋해 다음 일차에 재사용 가능하게 한다
// 웨이브는 정비 단계에서 플레이어가 시작 버튼을 눌렀을 때만 진행된다
// 웨이브 클리어 이후의 단계 전환은 GameStageController가 결정한다
public class WaveManager : Singleton<WaveManager>
{
    // 인스펙터에서 WaveSet을 할당한다
    [SerializeField] private WaveSet waveSet;

    // 현재 진행 중인 웨이브 인덱스 (0부터 시작, -1이면 시작 전)
    private int currentWaveIndex = -1;

    // 현재 웨이브에서 살아있는 적 수
    private int aliveEnemyCount;
    private EnemyUnit pendingLastKilledEnemy;
    private WaveConfig pendingClearedWaveConfig;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => waveSet != null ? waveSet.WaveCount : 0;
    public bool IsLastWave => currentWaveIndex >= TotalWaves - 1;

    public bool TryGetUpcomingWave(out WaveConfig waveConfig)
    {
        waveConfig = null;

        if (waveSet == null)
            return false;

        int nextWaveIndex = currentWaveIndex + 1;
        if (nextWaveIndex < 0 || nextWaveIndex >= waveSet.WaveCount)
            return false;

        waveConfig = waveSet.Waves[nextWaveIndex];
        return waveConfig != null;
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Subscribe<EnemyKillRewardAbsorbedEvent>(OnEnemyKillRewardAbsorbed);
        EventBus.Instance.Subscribe<StageChangedEvent>(OnStageChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Unsubscribe<EnemyKillRewardAbsorbedEvent>(OnEnemyKillRewardAbsorbed);
        EventBus.Instance.Unsubscribe<StageChangedEvent>(OnStageChanged);
    }

    private void OnStageChanged(StageChangedEvent e)
    {
        if (e.CurrentStage == StageType.Preparation)
        {
            // 전투 시작 전 초기 상태이거나 마지막 웨이브까지 끝난 뒤에만 리셋한다.
            // 중간 웨이브 사이 정비 단계에서는 현재 웨이브 인덱스를 유지해야 다음 웨이브가 시작된다.
            if (currentWaveIndex < 0 || IsLastWave)
                ResetForNewDay();
        }
        else if (e.CurrentStage == StageType.Battle)
        {
            StartNextWave();
        }
    }

    // 다음 웨이브를 시작한다
    private void StartNextWave()
    {
        currentWaveIndex++;

        if (waveSet == null)
        {
            Debug.LogError("WaveManager: WaveSet이 할당되지 않았습니다. 인스펙터에서 WaveSet을 연결해주세요.");
            return;
        }

        if (currentWaveIndex >= waveSet.WaveCount)
        {
            Debug.LogError("WaveManager: 모든 웨이브가 소진되었는데 다시 시작하려 합니다.");
            return;
        }

        WaveConfig waveConfig = waveSet.Waves[currentWaveIndex];
        SpawnEnemies(waveConfig);

        EventBus.Instance.Publish(new WaveStartedEvent
        {
            WaveNumber = CurrentWaveNumber,
            TotalWaves = TotalWaves
        });
    }

    // 웨이브 구성을 기반으로 UnitRegistry 풀에서 적을 스폰한다
    private void SpawnEnemies(WaveConfig waveConfig)
    {
        aliveEnemyCount = 0;

        foreach (WaveSpawnEntry entry in waveConfig.spawnEntries)
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
        {
            pendingLastKilledEnemy = e.Unit;
            pendingClearedWaveConfig = waveSet.Waves[currentWaveIndex];

            if (e.DarkEnergyReward <= 0)
                HandleWaveCleared();
        }
    }

    private void OnEnemyKillRewardAbsorbed(EnemyKillRewardAbsorbedEvent e)
    {
        if (pendingClearedWaveConfig == null)
            return;

        if (pendingLastKilledEnemy != null && e.Unit != pendingLastKilledEnemy)
            return;

        HandleWaveCleared();
    }

    // 웨이브 클리어 처리를 수행한다
    private void HandleWaveCleared()
    {
        WaveConfig waveConfig = pendingClearedWaveConfig != null
            ? pendingClearedWaveConfig
            : waveSet.Waves[currentWaveIndex];

        pendingLastKilledEnemy = null;
        pendingClearedWaveConfig = null;

        EventBus.Instance.Publish(new WaveClearedEvent
        {
            WaveNumber = CurrentWaveNumber,
            DarkEnergyReward = waveConfig.darkEnergyReward
        });

        // 다음 웨이브 시작 또는 승리 처리는 GameStageController가 WaveClearedEvent를 받아 결정한다
    }

    // 새 일차 시작 전 웨이브 상태를 초기화한다
    private void ResetForNewDay()
    {
        currentWaveIndex = -1;
        aliveEnemyCount = 0;
        pendingLastKilledEnemy = null;
        pendingClearedWaveConfig = null;
    }
}
