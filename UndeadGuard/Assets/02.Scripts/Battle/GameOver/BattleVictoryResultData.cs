using System;

[Serializable]
public class BattleVictoryResultData
{
    public int clearedWave;
    public int totalWaves;
    public int totalTurns;
    public int survivingUndeadCount;
    public int lostUndeadCount;
    public int killedEnemyCount;
    public int remainingCoreHp;
    public int maxCoreHp;
    public int usedDarkEnergy;
    public string topKillerUnitName;
    public string mvpUnitName;
    public string MvpUndeadName;
    public int MvpUndeadKillCount;
}
