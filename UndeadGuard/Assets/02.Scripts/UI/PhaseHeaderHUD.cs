using UnityEngine;
using UnityEngine.UIElements;

public class PhaseHeaderHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private CoreHealth coreHealth;

    [Header("UXML Names")]
    [SerializeField] private string dayLabelName = "DayLabel";
    [SerializeField] private string phaseTypeLabelName = "PhaseTypeLabel";
    [SerializeField] private string roundBoxName = "RoundBox";
    [SerializeField] private string roundLabelName = "RoundLabel";
    [SerializeField] private string darkEnergyLabelName = "DarkEnergyLabel";
    [SerializeField] private string phaseSubInfoLabelName = "PhaseSubInfoLabel";
    [SerializeField] private string phaseEndButtonName = "PhaseEndButton";

    private Label dayLabel;
    private Label phaseTypeLabel;
    private VisualElement roundBox;
    private Label roundLabel;
    private Label darkEnergyLabel;
    private Label phaseSubInfoLabel;
    private Button phaseEndButton;

    private StageType CurrentStage;
    private int currentDarkEnergy;
    private int currentCommandPoint;
    private int currentCoreHp;
    private int maxCoreHp;

    private void OnEnable()
    {
        CurrentStage = GameStageController.Instance != null
            ? GameStageController.Instance.CurrentStage
            : StageType.Preparation;

        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument is null");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        dayLabel = root.Q<Label>(dayLabelName);
        phaseTypeLabel = root.Q<Label>(phaseTypeLabelName);
        roundBox = root.Q<VisualElement>(roundBoxName);
        roundLabel = root.Q<Label>(roundLabelName);
        darkEnergyLabel = root.Q<Label>(darkEnergyLabelName);
        phaseSubInfoLabel = root.Q<Label>(phaseSubInfoLabelName);
        phaseEndButton = root.Q<Button>(phaseEndButtonName);

        if (phaseEndButton != null)
            phaseEndButton.RegisterCallback<ClickEvent>(OnPhaseEndClicked);

        EventBus.Instance.Subscribe<StageChangedEvent>(OnStageChanged);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<ResourceChangedEvent>(OnResourceChanged);
        EventBus.Instance.Subscribe<CoreHealthChangedEvent>(OnCoreHealthChanged);
        EventBus.Instance.Subscribe<WaveStartedEvent>(OnWaveStarted);

        CacheInitialValues();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (phaseEndButton != null)
            phaseEndButton.UnregisterCallback<ClickEvent>(OnPhaseEndClicked);

        EventBus.Instance.Unsubscribe<StageChangedEvent>(OnStageChanged);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
        EventBus.Instance.Unsubscribe<CoreHealthChangedEvent>(OnCoreHealthChanged);
        EventBus.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
    }

    private void CacheInitialValues()
    {
        if (ResourceManager.Instance != null)
        {
            currentDarkEnergy = ResourceManager.Instance.DarkEnergy;
            currentCommandPoint = ResourceManager.Instance.CommandPoints;
        }

        if (coreHealth != null)
        {
            currentCoreHp = coreHealth.CurrentHp;
            maxCoreHp = coreHealth.MaxHp;
        }
    }

    private void OnStageChanged(StageChangedEvent e)
    {
        CurrentStage = e.CurrentStage;
        RefreshAll();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (CurrentStage != StageType.Battle)
            return;

        if (phaseEndButton != null)
            phaseEndButton.SetEnabled(e.CurrentTurn == TurnType.Player);

        RefreshBattleHeader();
    }

    private void OnResourceChanged(ResourceChangedEvent e)
    {
        currentDarkEnergy = e.DarkEnergy;
        currentCommandPoint = e.CommandPoint;
        RefreshResourceTexts();
    }

    private void OnCoreHealthChanged(CoreHealthChangedEvent e)
    {
        currentCoreHp = e.CurrentHp;
        maxCoreHp = e.MaxHp;

        if (CurrentStage == StageType.Preparation)
            RefreshPreparationHeader();
    }

    private void OnWaveStarted(WaveStartedEvent e)
    {
        RefreshAll();
    }

    private void OnPhaseEndClicked(ClickEvent e)
    {
        if (CurrentStage == StageType.Preparation)
        {
            if (GameStageController.Instance != null)
                GameStageController.Instance.RequestNextWave();
            return;
        }

        if (CurrentStage == StageType.Battle)
        {
            EventBus.Instance.Publish(new EndTurnRequestedEvent());
        }
    }

    private void RefreshAll()
    {
        if (CurrentStage == StageType.Preparation)
            RefreshPreparationHeader();
        else
            RefreshBattleHeader();
    }

    private void RefreshPreparationHeader()
    {
        int dayNumber = GetDisplayWaveNumber();

        if (dayLabel != null)
            dayLabel.text = $"{dayNumber}일차";

        if (phaseTypeLabel != null)
            phaseTypeLabel.text = "정비";

        if (roundBox != null)
            roundBox.style.display = DisplayStyle.None;

        if (roundLabel != null)
            roundLabel.text = string.Empty;

        if (darkEnergyLabel != null)
            darkEnergyLabel.text = $"Dark Energy : {currentDarkEnergy}";

        if (phaseSubInfoLabel != null)
            phaseSubInfoLabel.text = $"Core HP : {currentCoreHp} / {maxCoreHp}";

        if (phaseEndButton != null)
            phaseEndButton.text = "정비 완료";
    }

    private void RefreshBattleHeader()
    {
        int dayNumber = GetDisplayWaveNumber();
        int roundNumber = Mathf.Max(1, GameProgress.Instance.CurrentRound);

        if (dayLabel != null)
            dayLabel.text = $"{dayNumber}일차";

        if (phaseTypeLabel != null)
            phaseTypeLabel.text = "배틀";

        if (roundBox != null)
            roundBox.style.display = DisplayStyle.Flex;

        if (roundLabel != null)
            roundLabel.text = $"라운드 : {roundNumber}" ;

        if (darkEnergyLabel != null)
            darkEnergyLabel.text = $"Dark Energy : {currentDarkEnergy}";

        if (phaseSubInfoLabel != null)
            phaseSubInfoLabel.text = $"Command Point : {currentCommandPoint}";

        if (phaseEndButton != null)
            phaseEndButton.text = "턴 종료";
    }

    private void RefreshResourceTexts()
    {
        if (darkEnergyLabel != null)
            darkEnergyLabel.text = $"Dark Energy : {currentDarkEnergy}";

        if (CurrentStage == StageType.Preparation)
        {
            if (phaseSubInfoLabel != null)
                phaseSubInfoLabel.text = $"Core HP : {currentCoreHp} / {maxCoreHp}";
        }
        else
        {
            if (phaseSubInfoLabel != null)
                phaseSubInfoLabel.text = $"Command Point : {currentCommandPoint}";
        }
    }

    private int GetDisplayWaveNumber()
    {
        if (waveManager == null)
            return 1;

        return Mathf.Max(1, waveManager.CurrentWaveNumber);
    }
}

