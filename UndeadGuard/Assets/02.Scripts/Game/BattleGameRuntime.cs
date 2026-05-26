using UnityEngine;

// 배틀 씬에 들어왔을 때만 살아있는 전투 진행 런타임.
// GameManager가 생성/정리하며, 하이라키 오브젝트를 필요로 하지 않는다.
public sealed class BattleGameRuntime
{
    private readonly GameProgress gameProgress;
    private readonly GameStageController stageController;
    private bool isCleanedUp;

    public BattleGameRuntime()
    {
        gameProgress = new GameProgress();
        stageController = new GameStageController();
    }

    public void Begin()
    {
        if (isCleanedUp)
            return;

        if (GameStageController.Instance != null)
            GameStageController.Instance.Begin();
        else
            Debug.LogError("[BattleGameRuntime] GameStageController instance is null at Begin.");
    }

    public void Cleanup()
    {
        if (isCleanedUp)
            return;

        isCleanedUp = true;
        stageController?.Cleanup();
        gameProgress?.Cleanup();
    }
}
