using System.Collections;
using UnityEngine;

public sealed class GameOverController : MonoBehaviour
{
    public static GameOverResultData LastResultData { get; private set; }

    [Header("Scene Flow")]
    [SerializeField] private AppFlowSceneConfig sceneConfig;
    [SerializeField] private string resultSceneNameOverride;

    [Header("Stats")]
    [SerializeField] private BattleStatsTracker statsTracker;

    [Header("Timing")]
    [SerializeField] private float resultOpenDelaySeconds = 0.45f;

    private bool hasRequestedResult;
    private Coroutine openResultRoutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        LastResultData = null;
        hasRequestedResult = false;
        EventBus.Instance.Subscribe<CoreDestroyedEvent>(OnCoreDestroyed);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<CoreDestroyedEvent>(OnCoreDestroyed);

        if (openResultRoutine != null)
        {
            StopCoroutine(openResultRoutine);
            openResultRoutine = null;
        }
    }

    private void ResolveReferences()
    {
        if (statsTracker == null)
            statsTracker = FindFirstObjectByType<BattleStatsTracker>();
    }

    private void OnCoreDestroyed(CoreDestroyedEvent e)
    {
        if (hasRequestedResult)
            return;

        hasRequestedResult = true;

        GameOverResultData resultData = CreateResultData();
        LastResultData = resultData;

        openResultRoutine = StartCoroutine(OpenResultAfterDelay(resultData));
    }

    private IEnumerator OpenResultAfterDelay(GameOverResultData resultData)
    {
        float delay = Mathf.Max(0f, resultOpenDelaySeconds);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        openResultRoutine = null;

        EventBus.Instance.Publish(new RequestOpenResultEvent
        {
            SceneName = ResolveResultSceneName(),
            ResultData = resultData
        });
    }

    private GameOverResultData CreateResultData()
    {
        int survivedDay = GameProgress.Instance != null ? GameProgress.Instance.CurrentDay : 1;
        int reachedWave = ResolveReachedWave();

        GameOverResultData resultData = statsTracker != null
            ? statsTracker.CreateGameOverResultData(survivedDay, reachedWave)
            : new GameOverResultData
            {
                survivedDay = Mathf.Max(1, survivedDay),
                reachedWave = Mathf.Max(1, reachedWave),
                lastAttackerName = "\uC54C \uC218 \uC5C6\uC74C"
            };

        ApplyEvaluation(resultData);
        return resultData;
    }

    private static int ResolveReachedWave()
    {
        if (WaveManager.Instance != null)
            return Mathf.Max(1, WaveManager.Instance.CurrentWaveNumber);

        if (GameProgress.Instance != null)
            return Mathf.Max(1, GameProgress.Instance.CurrentPhaseIndex);

        return 1;
    }

    private void ApplyEvaluation(GameOverResultData resultData)
    {
        if (resultData == null)
            return;

        int survivedDay = Mathf.Max(1, resultData.survivedDay);
        int reachedWave = Mathf.Max(1, resultData.reachedWave);
        int defeatedEnemies = Mathf.Max(0, resultData.killedEnemyCount);

        if (survivedDay >= 3 || defeatedEnemies >= 20)
        {
            resultData.lordEvaluationTitle = "\uB05D\uAE4C\uC9C0 \uBC84\uD2F4 \uBC29\uC5B4\uC120";
            resultData.lordEvaluationDescription = "\uCF54\uC5B4\uB294 \uD30C\uAD34\uB418\uC5C8\uC9C0\uB9CC, \uAD70\uC8FC\uC758 \uBC29\uC5B4\uC120\uC740 \uC624\uB798 \uBC84\uD168\uC2B5\uB2C8\uB2E4.";
            resultData.lordEvaluationHint = "\uD6C4\uBC29\uC744 \uACC4\uC18D \uBCF4\uD638\uD558\uACE0, \uB9C8\uC9C0\uB9C9 \uC6E8\uC774\uBE0C \uC804\uC5D0 \uC554\uD751 \uC5D0\uB108\uC9C0\uB97C \uC801\uADF9\uC801\uC73C\uB85C \uC0AC\uC6A9\uD558\uC138\uC694.";
            return;
        }

        if (reachedWave >= 3 || defeatedEnemies >= 8)
        {
            resultData.lordEvaluationTitle = "\uAC00\uB2A5\uC131\uC744 \uBCF4\uC778 \uBC29\uC5B4";
            resultData.lordEvaluationDescription = "\uBC29\uC5B4\uC758 \uD750\uB984\uC740 \uC7A1\uC558\uC9C0\uB9CC, \uCF54\uC5B4\uAC00 \uB108\uBB34 \uB9CE\uC740 \uC555\uBC15\uC744 \uBC1B\uC558\uC2B5\uB2C8\uB2E4.";
            resultData.lordEvaluationHint = "\uBC29\uD328\uBCD1\uC73C\uB85C \uC801\uC758 \uACBD\uB85C\uB97C \uBB36\uACE0, \uC57D\uD574\uC9C4 \uC801\uC744 \uBA3C\uC800 \uC815\uB9AC\uD558\uC138\uC694.";
            return;
        }

        resultData.lordEvaluationTitle = "\uBB34\uB108\uC9C4 \uC9C4\uD615";
        resultData.lordEvaluationDescription = "\uC5B8\uB370\uB4DC \uC804\uC5F4\uC774 \uC790\uB9AC\uB97C \uC7A1\uAE30 \uC804\uC5D0 \uBC29\uC5B4\uC120\uC774 \uBB34\uB108\uC84C\uC2B5\uB2C8\uB2E4.";
        resultData.lordEvaluationHint = "\uB0B4\uAD6C\uB825\uC774 \uB192\uC740 \uC720\uB2DB\uC744 \uCF54\uC5B4 \uADFC\uCC98\uC5D0 \uBC30\uCE58\uD558\uACE0, \uAC00\uC7A5 \uAC00\uAE4C\uC6B4 \uACF5\uACA9\uC790\uBD80\uD130 \uC9D1\uC911 \uACF5\uACA9\uD558\uC138\uC694.";
    }

    private string ResolveResultSceneName()
    {
        if (!string.IsNullOrWhiteSpace(resultSceneNameOverride))
            return resultSceneNameOverride;

        return sceneConfig != null ? sceneConfig.ResultSceneName : string.Empty;
    }
}
