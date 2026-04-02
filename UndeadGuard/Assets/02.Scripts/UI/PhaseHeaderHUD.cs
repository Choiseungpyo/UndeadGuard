using UnityEngine;
using UnityEngine.UIElements;

// �غ� �ܰ�� ���� �ܰ��� ��� ��� UI�� �����Ѵ�
// �ܰ� ����, �ڿ� ����, ���� ��ư �ؽ�Ʈ�� ���� ���� ���¿� �°� �����Ѵ�
public class PhaseHeaderHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private CoreHealth coreHealth;

    [Header("UXML Names")]
    [SerializeField] private string phaseTimeLabelName = "PhaseTimeLabel";
    [SerializeField] private string phaseInfoALabelName = "PhaseInfoALabel";
    [SerializeField] private string phaseInfoBBoxName = "PhaseInfoBBox";
    [SerializeField] private string phaseInfoBLabelName = "PhaseInfoBLabel";
    [SerializeField] private string darkEnergyLabelName = "DarkEnergyLabel";
    [SerializeField] private string phaseSubInfoLabelName = "PhaseSubInfoLabel";
    [SerializeField] private string phaseEndButtonName = "PhaseEndButton";

    [Header("Initial State")]
    [SerializeField] private PhaseType initialPhase = PhaseType.Preparation;

    private Label phaseTimeLabel;
    private Label phaseInfoALabel;
    private VisualElement phaseInfoBBox;
    private Label phaseInfoBLabel;
    private Label darkEnergyLabel;
    private Label phaseSubInfoLabel;
    private Button phaseEndButton;

    private PhaseType currentPhase;
    private int currentDarkEnergy;
    private int currentCommandPoint;
    private int currentCoreHp;
    private int maxCoreHp;

    private void OnEnable()
    {
        currentPhase = initialPhase;

        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument ������Ʈ�� ã�� �� �����ϴ�.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        phaseTimeLabel = root.Q<Label>(phaseTimeLabelName);
        phaseInfoALabel = root.Q<Label>(phaseInfoALabelName);
        phaseInfoBBox = root.Q<VisualElement>(phaseInfoBBoxName);
        phaseInfoBLabel = root.Q<Label>(phaseInfoBLabelName);
        darkEnergyLabel = root.Q<Label>(darkEnergyLabelName);
        phaseSubInfoLabel = root.Q<Label>(phaseSubInfoLabelName);
        phaseEndButton = root.Q<Button>(phaseEndButtonName);

        if (phaseEndButton != null)
            phaseEndButton.RegisterCallback<ClickEvent>(OnPhaseEndClicked);

        EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
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

        EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
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

    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        currentPhase = e.CurrentPhase;
        RefreshAll();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (currentPhase != PhaseType.Battle)
            return;

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

        if (currentPhase == PhaseType.Preparation)
            RefreshPreparationHeader();
    }

    private void OnWaveStarted(WaveStartedEvent e)
    {
        RefreshAll();
    }

    private void OnPhaseEndClicked(ClickEvent e)
    {
        if (currentPhase == PhaseType.Preparation)
        {
            if (GamePhaseController.Instance != null)
                GamePhaseController.Instance.RequestNextWave();
            return;
        }

        if (currentPhase == PhaseType.Battle)
        {
            EventBus.Instance.Publish(new EndTurnRequestedEvent());
        }
    }

    private void RefreshAll()
    {
        if (currentPhase == PhaseType.Preparation)
            RefreshPreparationHeader();
        else
            RefreshBattleHeader();
    }

    private void RefreshPreparationHeader()
    {
        int dayNumber = GetDisplayWaveNumber();

        if (phaseTimeLabel != null)
            phaseTimeLabel.text = $"�� {dayNumber}����";

        if (phaseInfoALabel != null)
            phaseInfoALabel.text = "��ġ";

        if (phaseInfoBBox != null)
            phaseInfoBBox.style.display = DisplayStyle.None;

        if (phaseInfoBLabel != null)
            phaseInfoBLabel.text = string.Empty;

        if (darkEnergyLabel != null)
            darkEnergyLabel.text = $"Dark Energy : {currentDarkEnergy}";

        if (phaseSubInfoLabel != null)
            phaseSubInfoLabel.text = $"Core HP : {currentCoreHp} / {maxCoreHp}";

        if (phaseEndButton != null)
            phaseEndButton.text = "��ġ ����";
    }

    private void RefreshBattleHeader()
    {
        int nightNumber = GetDisplayWaveNumber();
        int roundNumber = Mathf.Max(1, GameProgressTracker.Instance.CurrentRound);

        if (phaseTimeLabel != null)
            phaseTimeLabel.text = $"�� {nightNumber}";

        if (phaseInfoALabel != null)
            phaseInfoALabel.text = $"�� {roundNumber}";

        if (phaseInfoBBox != null)
            phaseInfoBBox.style.display = DisplayStyle.Flex;

        if (phaseInfoBLabel != null)
            phaseInfoBLabel.text = "����";

        if (darkEnergyLabel != null)
            darkEnergyLabel.text = $"Dark Energy : {currentDarkEnergy}";

        if (phaseSubInfoLabel != null)
            phaseSubInfoLabel.text = $"�������Ʈ : {currentCommandPoint}";

        if (phaseEndButton != null)
            phaseEndButton.text = "�� ����";
    }

    private void RefreshResourceTexts()
    {
        if (darkEnergyLabel != null)
            darkEnergyLabel.text = $"Dark Energy : {currentDarkEnergy}";

        if (currentPhase == PhaseType.Preparation)
        {
            if (phaseSubInfoLabel != null)
                phaseSubInfoLabel.text = $"Core HP : {currentCoreHp} / {maxCoreHp}";
        }
        else
        {
            if (phaseSubInfoLabel != null)
                phaseSubInfoLabel.text = $"�������Ʈ : {currentCommandPoint}";
        }
    }

    private int GetDisplayWaveNumber()
    {
        if (waveManager == null)
            return 1;

        return Mathf.Max(1, waveManager.CurrentWaveNumber);
    }
}
