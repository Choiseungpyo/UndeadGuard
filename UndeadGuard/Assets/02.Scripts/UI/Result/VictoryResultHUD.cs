using UnityEngine;
using UnityEngine.UIElements;

public sealed class VictoryResultHUD : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private ResultFlowEntry resultFlowEntry;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite titleDecorationSprite;
    [SerializeField] private Sprite battleRecordPanelSprite;
    [SerializeField] private Sprite legionRecordPanelSprite;
    [SerializeField] private Sprite mvpPanelSprite;
    [SerializeField] private Sprite defaultMvpPortrait;
    [SerializeField] private Sprite lobbyButtonSprite;

    private Image victoryBackgroundImage;
    private Image victoryTitleDecoration;
    private VisualElement battleRecordPanelImage;
    private VisualElement legionRecordPanelImage;
    private VisualElement mvpUndeadPanelImage;
    private Label victoryTitleLabel;
    private Label victorySubtitleLabel;
    private Label clearedWaveValueLabel;
    private Label totalTurnsValueLabel;
    private Label survivingUndeadCountValueLabel;
    private Label lostUndeadCountValueLabel;
    private Label killedEnemyCountValueLabel;
    private Label coreHpValueLabel;
    private Label usedDarkEnergyValueLabel;
    private Label legionRecordTitleLabel;
    private Label mvpTitleLabel;
    private Image mvpPortrait;
    private Label mvpNameLabel;
    private Label mvpKillCountLabel;
    private Button lobbyButton;
    private VisualElement lobbyButtonImage;
    private VisualElement victoryRoot;
    private bool isBound;
    private Coroutine pendingShowCoroutine;
    [SerializeField] private float showDelayAfterCameraReturn = 0.05f;

    private void Awake()
    {
        ResolveReferences();
        BindVisualElements();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindVisualElements();
        RegisterEvents();
        EventBus.Instance.Subscribe<BattleWonEvent>(OnBattleWon);
        Hide();
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<BattleWonEvent>(OnBattleWon);
        StopPendingShow();
        UnregisterEvents();
    }

    public void Show(BattleVictoryResultData data)
    {
        ApplyResult(data);
        SetVisible(true);
    }

    public void ApplyResult(BattleVictoryResultData data)
    {
        if (data == null)
            data = CreateFallbackData();

        SetLabel(victoryTitleLabel, "\uC2B9\uB9AC");
        SetLabel(victorySubtitleLabel, "\uBAA8\uB4E0 \uC6E8\uC774\uBE0C\uB97C \uB9C9\uC544\uB0C8\uC2B5\uB2C8\uB2E4.");
        SetLabel(clearedWaveValueLabel, $"{Mathf.Max(0, data.clearedWave)} / {Mathf.Max(0, data.totalWaves)}");
        SetLabel(totalTurnsValueLabel, Mathf.Max(0, data.totalTurns).ToString());
        SetLabel(survivingUndeadCountValueLabel, Mathf.Max(0, data.survivingUndeadCount).ToString());
        SetLabel(lostUndeadCountValueLabel, Mathf.Max(0, data.lostUndeadCount).ToString());
        SetLabel(killedEnemyCountValueLabel, Mathf.Max(0, data.killedEnemyCount).ToString());
        SetLabel(coreHpValueLabel, $"{Mathf.Max(0, data.remainingCoreHp)} / {Mathf.Max(0, data.maxCoreHp)}");
        SetLabel(usedDarkEnergyValueLabel, Mathf.Max(0, data.usedDarkEnergy).ToString());
        SetLabel(legionRecordTitleLabel, "\uAD70\uB2E8 \uAE30\uB85D");
        SetLabel(mvpTitleLabel, "\uCD5C\uB2E4 \uCC98\uCE58 \uC5B8\uB370\uB4DC");
        SetLabel(mvpNameLabel, ResolveText(data.MvpUndeadName, "\uAE30\uB85D \uC5C6\uC74C"));
        SetLabel(mvpKillCountLabel, $"{Mathf.Max(0, data.MvpUndeadKillCount)} \uCC98\uCE58");

        if (mvpPortrait != null)
            mvpPortrait.sprite = defaultMvpPortrait;

        ApplySprites();
    }

    private void ResolveReferences()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (resultFlowEntry == null)
            resultFlowEntry = GetComponent<ResultFlowEntry>();
    }

    private void BindVisualElements()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null)
            return;

        VisualElement root = uiDocument.rootVisualElement;
        victoryBackgroundImage = root.Q<Image>("VictoryBackgroundImage");
        victoryTitleDecoration = root.Q<Image>("VictoryTitleDecoration");
        battleRecordPanelImage = root.Q<VisualElement>("BattleRecordPanelImage");
        legionRecordPanelImage = root.Q<VisualElement>("LegionRecordPanelImage");
        mvpUndeadPanelImage = root.Q<VisualElement>("MvpUndeadPanelImage");
        victoryTitleLabel = root.Q<Label>("VictoryTitleLabel");
        victorySubtitleLabel = root.Q<Label>("VictorySubtitleLabel");
        clearedWaveValueLabel = root.Q<Label>("ClearedWaveValueLabel");
        totalTurnsValueLabel = root.Q<Label>("TotalTurnsValueLabel");
        survivingUndeadCountValueLabel = root.Q<Label>("SurvivingUndeadCountValueLabel");
        lostUndeadCountValueLabel = root.Q<Label>("LostUndeadCountValueLabel");
        killedEnemyCountValueLabel = root.Q<Label>("KilledEnemyCountValueLabel");
        coreHpValueLabel = root.Q<Label>("CoreHpValueLabel");
        usedDarkEnergyValueLabel = root.Q<Label>("UsedDarkEnergyValueLabel");
        legionRecordTitleLabel = root.Q<Label>("LegionRecordTitleLabel");
        mvpTitleLabel = root.Q<Label>("MvpTitleLabel");
        mvpPortrait = root.Q<Image>("MvpPortrait");
        mvpNameLabel = root.Q<Label>("MvpNameLabel");
        mvpKillCountLabel = root.Q<Label>("MvpKillCountLabel");
        lobbyButton = root.Q<Button>("LobbyButton");
        lobbyButtonImage = root.Q<VisualElement>("LobbyButtonImage");
        victoryRoot = root.Q<VisualElement>("VictoryRoot");

        ApplySprites();
    }

    private void RegisterEvents()
    {
        if (isBound)
            return;

        if (lobbyButton != null)
            lobbyButton.clicked += OnLobbyRequested;

        isBound = true;
    }

    private void UnregisterEvents()
    {
        if (!isBound)
            return;

        if (lobbyButton != null)
            lobbyButton.clicked -= OnLobbyRequested;

        isBound = false;
    }

    private void OnLobbyRequested()
    {
        Hide();

        if (resultFlowEntry != null)
            resultFlowEntry.OnContinueToLobbyButtonPressed();
        else
            EventBus.Instance.Publish(new RequestOpenLobbyEvent());
    }

    private void OnBattleWon(BattleWonEvent e)
    {
        QueueShow(CreateVictoryData(e));
    }

    private void QueueShow(BattleVictoryResultData data)
    {
        StopPendingShow();
        pendingShowCoroutine = StartCoroutine(ShowAfterActionFlow(data));
    }

    private System.Collections.IEnumerator ShowAfterActionFlow(BattleVictoryResultData data)
    {
        BattleInputGuard guard = BattleInputGuard.Instance;
        if (guard != null)
            yield return new WaitUntil(() => guard == null || !guard.IsActionCameraActive());

        yield return null;

        float delay = Mathf.Max(0f, showDelayAfterCameraReturn);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Show(data);
        pendingShowCoroutine = null;
    }

    private void StopPendingShow()
    {
        if (pendingShowCoroutine == null)
            return;

        StopCoroutine(pendingShowCoroutine);
        pendingShowCoroutine = null;
    }

    private BattleVictoryResultData CreateVictoryData(BattleWonEvent e)
    {
        BattleStatsTracker statsTracker = FindFirstObjectByType<BattleStatsTracker>();
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();

        int survivingUndeadCount = UnitRegistry.Instance != null
            ? UnitRegistry.Instance.GetAliveUndeadUnits().Count
            : 0;
        int remainingCoreHp = CoreHealth.Instance != null ? CoreHealth.Instance.CurrentHp : 0;
        int maxCoreHp = CoreHealth.Instance != null ? CoreHealth.Instance.MaxHp : 0;
        int totalTurns = turnManager != null ? turnManager.RoundCount : 0;
        int clearedWave = e != null ? e.ClearedWaveNumber : (WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveNumber : 0);
        int totalWaves = e != null ? e.TotalWaves : (WaveManager.Instance != null ? WaveManager.Instance.TotalWaves : 0);

        if (statsTracker != null)
        {
            return statsTracker.CreateVictoryResultData(
                clearedWave,
                totalWaves,
                totalTurns,
                survivingUndeadCount,
                remainingCoreHp,
                maxCoreHp,
                0);
        }

        return new BattleVictoryResultData
        {
            clearedWave = Mathf.Max(0, clearedWave),
            totalWaves = Mathf.Max(0, totalWaves),
            totalTurns = Mathf.Max(0, totalTurns),
            survivingUndeadCount = Mathf.Max(0, survivingUndeadCount),
            remainingCoreHp = Mathf.Max(0, remainingCoreHp),
            maxCoreHp = Mathf.Max(0, maxCoreHp),
            MvpUndeadName = "\uAE30\uB85D \uC5C6\uC74C",
            MvpUndeadKillCount = 0
        };
    }

    private void ApplySprites()
    {
        SetSprite(victoryBackgroundImage, backgroundSprite);
        SetSprite(victoryTitleDecoration, titleDecorationSprite);
        SetBackgroundSprite(battleRecordPanelImage, battleRecordPanelSprite);
        SetBackgroundSprite(legionRecordPanelImage, legionRecordPanelSprite);
        SetBackgroundSprite(mvpUndeadPanelImage, mvpPanelSprite);
        SetSprite(mvpPortrait, defaultMvpPortrait);
        SetBackgroundSprite(lobbyButtonImage, lobbyButtonSprite);
        ApplyImageScaleModes();
    }

    private void ApplyImageScaleModes()
    {
        SetScaleMode(victoryBackgroundImage, ScaleMode.StretchToFill);
        SetScaleMode(victoryTitleDecoration, ScaleMode.StretchToFill);
        SetScaleMode(mvpPortrait, ScaleMode.ScaleToFit);
    }

    private static BattleVictoryResultData CreateFallbackData()
    {
        return new BattleVictoryResultData
        {
            MvpUndeadName = "\uAE30\uB85D \uC5C6\uC74C",
            MvpUndeadKillCount = 0
        };
    }

    private static void SetLabel(Label label, string text)
    {
        if (label != null)
            label.text = text;
    }

    private void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (victoryRoot != null)
            victoryRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static void SetSprite(Image image, Sprite sprite)
    {
        if (image != null && sprite != null)
            image.sprite = sprite;
    }

    private static void SetBackgroundSprite(VisualElement element, Sprite sprite)
    {
        if (element != null && sprite != null)
            element.style.backgroundImage = new StyleBackground(sprite);
    }

    private static void SetScaleMode(Image image, ScaleMode scaleMode)
    {
        if (image != null)
            image.scaleMode = scaleMode;
    }

    private static string ResolveText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
