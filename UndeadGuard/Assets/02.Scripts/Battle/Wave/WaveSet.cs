using System;
using System.Collections.Generic;
using UnityEngine;

// 한 일차의 모든 웨이브 구성을 담는 컨테이너 ScriptableObject
// WaveEditorWindow에서 편집하고 WaveManager에서 런타임에 사용한다
[CreateAssetMenu(fileName = "WaveSet", menuName = "Wave/Wave Set")]
public class WaveSet : ScriptableObject
{
    [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();

    public IReadOnlyList<WaveConfig> Waves => waves;
    public int WaveCount => waves.Count;

#if UNITY_EDITOR
    public List<WaveConfig> WaveList => waves;

    // 새 웨이브를 목록 끝에 추가한다
    public void AddWave()
    {
        waves.Add(new WaveConfig { darkEnergyReward = 3 });
    }

    // 지정 인덱스의 웨이브를 제거한다
    public void RemoveWave(int index)
    {
        if (index >= 0 && index < waves.Count)
            waves.RemoveAt(index);
    }
#endif
}

// 웨이브 하나의 구성 데이터 (보상 + 스폰 목록)
[Serializable]
public class WaveConfig
{
    public int darkEnergyReward = 3;
    public List<WaveSpawnEntry> spawnEntries = new List<WaveSpawnEntry>();
}
