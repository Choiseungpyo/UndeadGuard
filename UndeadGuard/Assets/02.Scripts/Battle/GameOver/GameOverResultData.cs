using System;

[Serializable]
public class GameOverResultData
{
    public int survivedDay;
    public int reachedWave;
    public int killedEnemyCount;
    public int lostUndeadCount;
    public int revivedUndeadCount;
    public int totalCoreDamageTaken;
    public string lastAttackerName;
    public string lordEvaluationTitle;
    public string lordEvaluationDescription;
    public string lordEvaluationHint;
}
