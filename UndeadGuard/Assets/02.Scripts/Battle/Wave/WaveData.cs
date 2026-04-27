using System.Collections.Generic;
using UnityEngine;

// 웨이브 하나의 적 스폰 정보를 담는 ScriptableObject
// 인스펙터에서 각 웨이브의 적 구성과 스폰 위치를 설정한다
[CreateAssetMenu(fileName = "WaveData", menuName = "Wave/Wave Data")]
public class WaveData : ScriptableObject
{
    // 이 웨이브의 번호
    [SerializeField] private int waveNumber;

    // 웨이브 클리어 시 지급하는 암흑 에너지 보상
    [SerializeField] private int darkEnergyReward = 3;

    // 이 웨이브에서 스폰할 적 목록
    [SerializeField] private List<WaveSpawnEntry> spawnEntries = new List<WaveSpawnEntry>();

    public int WaveNumber => waveNumber;
    public int DarkEnergyReward => darkEnergyReward;
    public IReadOnlyList<WaveSpawnEntry> SpawnEntries => spawnEntries;
}

// 웨이브 내 단일 적 스폰 정보
// 프리팹 대신 EnemyType으로 지정하며 실제 스폰은 UnitRegistry의 풀에서 처리한다
[System.Serializable]
public class WaveSpawnEntry
{
    // 스폰할 적 유닛 종류
    public EnemyType enemyType;

    // 스폰 그리드 좌표
    public Vector2Int spawnPosition;
}
