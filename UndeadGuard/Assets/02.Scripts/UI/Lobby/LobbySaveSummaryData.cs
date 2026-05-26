using System;

[Serializable]
public sealed class LobbySaveSummaryData
{
    public bool HasSaveData;
    public int Day = 1;
    public int CurrentWave = 1;
    public int CoreHp = 100;
    public int CoreMaxHp = 100;
    public int DarkEnergy;
    public string SelectedLordName = "사령 군주";
    public int OwnedUndeadCount;

    public LobbySaveSummaryData Clone()
    {
        return new LobbySaveSummaryData
        {
            HasSaveData = HasSaveData,
            Day = Day,
            CurrentWave = CurrentWave,
            CoreHp = CoreHp,
            CoreMaxHp = CoreMaxHp,
            DarkEnergy = DarkEnergy,
            SelectedLordName = SelectedLordName,
            OwnedUndeadCount = OwnedUndeadCount
        };
    }
}
