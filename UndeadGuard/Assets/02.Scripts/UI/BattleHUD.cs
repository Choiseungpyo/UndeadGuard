using UnityEngine;
using UnityEngine.UIElements;

// 전투 HUD 전체를 관리한다
// Turn, Round, Dark Energy, Command Point 텍스트 갱신 및 EndTurnButton 표시 여부를 담당한다
public class BattleHUD : MonoBehaviour
{
    // UI Builder에서 지정한 각 요소의 name 속성값
    [SerializeField] private string turnLabelName = "TurnLabel";
    [SerializeField] private string roundLabelName = "RoundLabel";
    [SerializeField] private string darkEnergyLabelName = "DarkEnergyLabel";
    [SerializeField] private string commandPointLabelName = "CommandPointLabel";
    [SerializeField] private string coreHpLabelName = "CoreHpLabel";
    [SerializeField] private string waveLabelName = "WaveLabel";
    [SerializeField] private string endTurnButtonName = "End_Turn_Button";
    [SerializeField] private string nextWaveButtonName = "NextWave_Button";

    private Label turnLabel;
    private Label roundLabel;
    private Label darkEnergyLabel;
    private Label commandPointLabel;
    private Label coreHpLabel;
    private Label waveLabel;
    private Button endTurnButton;
    private Button nextWaveButton;

    private void OnEnable()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        turnLabel = root.Q<Label>(turnLabelName);
        roundLabel = root.Q<Label>(roundLabelName);
        darkEnergyLabel = root.Q<Label>(darkEnergyLabelName);
        commandPointLabel = root.Q<Label>(commandPointLabelName);
        coreHpLabel = root.Q<Label>(coreHpLabelName);
        waveLabel = root.Q<Label>(waveLabelName);
        endTurnButton = root.Q<Button>(endTurnButtonName);
        nextWaveButton = root.Q<Button>(nextWaveButtonName);

        if (endTurnButton != null)
            endTurnButton.RegisterCallback<ClickEvent>(OnEndTurnClicked);

        if (nextWaveButton != null)
        {
            nextWaveButton.RegisterCallback<ClickEvent>(OnNextWaveClicked);
            nextWaveButton.style.display = DisplayStyle.None;
        }

        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<ResourceChangedEvent>(OnResourceChanged);
        EventBus.Instance.Subscribe<CoreHealthChangedEvent>(OnCoreHealthChanged);
        EventBus.Instance.Subscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);

        RefreshUI();
    }

    private void OnDisable()
    {
        if (endTurnButton != null)
            endTurnButton.UnregisterCallback<ClickEvent>(OnEndTurnClicked);

        if (nextWaveButton != null)
            nextWaveButton.UnregisterCallback<ClickEvent>(OnNextWaveClicked);

        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
        EventBus.Instance.Unsubscribe<CoreHealthChangedEvent>(OnCoreHealthChanged);
        EventBus.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
    }

    // 현재 상태 기준으로 HUD를 한 번 초기화한다
    private void RefreshUI()
    {
        UpdateTurnUI(false);
    }

    // 턴 변경 시 Turn 레이블과 End Turn 버튼 표시 여부를 갱신한다
    private void OnTurnChanged(TurnChangedEvent e)
    {
        bool isPlayerTurn = e.CurrentTurn == TurnType.Player;
        UpdateTurnUI(isPlayerTurn);

        if (roundLabel != null)
            roundLabel.text = $"Round {GameProgressTracker.Instance.CurrentRound}";
    }

    // 자원 변경 시 Dark Energy 및 Command Point 레이블을 갱신한다
    private void OnResourceChanged(ResourceChangedEvent e)
    {
        if (darkEnergyLabel != null)
            darkEnergyLabel.text = $"암흑 에너지: {e.DarkEnergy}";

        if (commandPointLabel != null)
            commandPointLabel.text = $"사령 포인트: {e.CommandPoint}";
    }

    // 코어 체력 변경 시 Core HP 레이블을 갱신한다
    private void OnCoreHealthChanged(CoreHealthChangedEvent e)
    {
        if (coreHpLabel != null)
            coreHpLabel.text = $"핵 HP: {e.CurrentHp} / {e.MaxHp}";
    }

    // 웨이브 시작 시 웨이브 레이블을 갱신한다
    private void OnWaveStarted(WaveStartedEvent e)
    {
        if (waveLabel != null)
            waveLabel.text = $"Wave {e.WaveNumber}";
    }

    // 페이즈 변경 시 버튼 표시 여부를 갱신한다
    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        bool isPreparation = e.CurrentPhase == PhaseType.Preparation;

        if (nextWaveButton != null)
        {
            nextWaveButton.style.display = isPreparation
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }

    // 턴 관련 UI를 갱신한다
    private void UpdateTurnUI(bool isPlayerTurn)
    {
        if (turnLabel != null)
            turnLabel.text = isPlayerTurn ? "플레이어 턴" : "적 턴";

        if (endTurnButton != null)
        {
            endTurnButton.style.display = isPlayerTurn
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }

    // End Turn 버튼 클릭 시 플레이어 턴을 종료한다
    private void OnEndTurnClicked(ClickEvent e)
    {
        EventBus.Instance.Publish(new EndTurnRequestedEvent());
    }

    // Next Wave 버튼 클릭 시 다음 웨이브를 시작한다
    private void OnNextWaveClicked(ClickEvent e)
    {
        if (GamePhaseController.Instance != null)
            GamePhaseController.Instance.RequestNextWave();
    }
}
