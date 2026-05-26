using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class LobbyMainMenuHUD : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Texture2D bottomLayoutTexture;

    [Header("Flow")]
    [SerializeField] private LobbyFlowEntry lobbyFlowEntry;

    [Header("Initial Data")]
    [SerializeField] private LobbySaveSummaryData initialSummary = new LobbySaveSummaryData();

    private VisualElement root;

    private Button newGameButton;
    private Button continueButton;
    private Button quitButton;
    private Button creditsIconButton;
    private Button settingsIconButton;
    private Image bottomLayoutImage;

    private VisualElement operationInfoPanel;
    private Label dayLabel;
    private Label waveLabel;
    private Label coreHpLabel;
    private Label darkEnergyLabel;
    private Label selectedLordLabel;
    private Label undeadCountLabel;

    private VisualElement newGameConfirmOverlay;
    private Label newGameConfirmTitle;
    private Label newGameConfirmMessage;
    private Button newGameConfirmOkButton;
    private Button newGameConfirmCancelButton;

    private LobbySaveSummaryData saveSummary;
    private bool isBound;

    private void Awake()
    {
        ResolveReferences();
        BindVisualElements();
        Initialize(initialSummary != null ? initialSummary.Clone() : new LobbySaveSummaryData());
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindVisualElements();
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    public void Initialize(LobbySaveSummaryData summary)
    {
        saveSummary = summary ?? new LobbySaveSummaryData();

        bool hasSave = saveSummary.HasSaveData;
        if (continueButton != null)
            continueButton.SetEnabled(hasSave);

        if (operationInfoPanel != null)
            operationInfoPanel.style.display = hasSave ? DisplayStyle.Flex : DisplayStyle.None;

        HideNewGameConfirm();

        if (hasSave)
            ApplyOperationInfo(saveSummary);
    }

    private void ResolveReferences()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (lobbyFlowEntry == null)
            lobbyFlowEntry = GetComponent<LobbyFlowEntry>();
    }

    private void BindVisualElements()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[LobbyMainMenuHUD] UIDocument is not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning("[LobbyMainMenuHUD] UIDocument root is not ready.");
            return;
        }

        newGameButton = root.Q<Button>("NewGameButton");
        continueButton = root.Q<Button>("ContinueButton");
        quitButton = root.Q<Button>("QuitButton");

        creditsIconButton = root.Q<Button>("CreditsIconButton");
        settingsIconButton = root.Q<Button>("SettingsIconButton");
        bottomLayoutImage = root.Q<Image>("BottomLayout");
        if (bottomLayoutImage != null)
        {
            bottomLayoutImage.image = bottomLayoutTexture;
            bottomLayoutImage.scaleMode = ScaleMode.StretchToFill;
        }

        operationInfoPanel = root.Q<VisualElement>("OperationInfoPanel");
        dayLabel = root.Q<Label>("DayLabel");
        waveLabel = root.Q<Label>("WaveLabel");
        coreHpLabel = root.Q<Label>("CoreHpLabel");
        darkEnergyLabel = root.Q<Label>("DarkEnergyLabel");
        selectedLordLabel = root.Q<Label>("SelectedLordLabel");
        undeadCountLabel = root.Q<Label>("UndeadCountLabel");

        newGameConfirmOverlay = root.Q<VisualElement>("NewGameConfirmOverlay");
        newGameConfirmTitle = root.Q<Label>("NewGameConfirmTitle");
        newGameConfirmMessage = root.Q<Label>("NewGameConfirmMessage");
        newGameConfirmOkButton = root.Q<Button>("NewGameConfirmOkButton");
        newGameConfirmCancelButton = root.Q<Button>("NewGameConfirmCancelButton");
    }

    private void RegisterEvents()
    {
        if (isBound)
            return;

        if (newGameButton != null)
            newGameButton.clicked += OnNewGameClicked;
        if (continueButton != null)
            continueButton.clicked += OnContinueClicked;
        if (quitButton != null)
            quitButton.clicked += OnQuitClicked;
        if (creditsIconButton != null)
            creditsIconButton.clicked += OnCreditsClicked;
        if (settingsIconButton != null)
            settingsIconButton.clicked += OnSettingsClicked;
        if (newGameConfirmOkButton != null)
            newGameConfirmOkButton.clicked += OnNewGameConfirmOkClicked;
        if (newGameConfirmCancelButton != null)
            newGameConfirmCancelButton.clicked += HideNewGameConfirm;

        isBound = true;
    }

    private void UnregisterEvents()
    {
        if (!isBound)
            return;

        if (newGameButton != null)
            newGameButton.clicked -= OnNewGameClicked;
        if (continueButton != null)
            continueButton.clicked -= OnContinueClicked;
        if (quitButton != null)
            quitButton.clicked -= OnQuitClicked;
        if (creditsIconButton != null)
            creditsIconButton.clicked -= OnCreditsClicked;
        if (settingsIconButton != null)
            settingsIconButton.clicked -= OnSettingsClicked;
        if (newGameConfirmOkButton != null)
            newGameConfirmOkButton.clicked -= OnNewGameConfirmOkClicked;
        if (newGameConfirmCancelButton != null)
            newGameConfirmCancelButton.clicked -= HideNewGameConfirm;

        isBound = false;
    }

    private void ApplyOperationInfo(LobbySaveSummaryData data)
    {
        SetLabel(dayLabel, $"밤 {Mathf.Max(1, data.Day)}일차");
        SetLabel(waveLabel, $"현재 웨이브: {Mathf.Max(1, data.CurrentWave)}");
        SetLabel(coreHpLabel, $"핵 체력: {Mathf.Max(0, data.CoreHp)} / {Mathf.Max(1, data.CoreMaxHp)}");
        SetLabel(darkEnergyLabel, $"암흑 에너지: {Mathf.Max(0, data.DarkEnergy)}");
        SetLabel(selectedLordLabel, $"선택 군주: {ResolveLordName(data.SelectedLordName)}");
        SetLabel(undeadCountLabel, $"보유 언데드: {Mathf.Max(0, data.OwnedUndeadCount)}");
    }

    private static void SetLabel(Label label, string text)
    {
        if (label != null)
            label.text = text;
    }

    private static string ResolveLordName(string lordName)
    {
        return string.IsNullOrWhiteSpace(lordName) ? "미지정" : lordName;
    }

    private void OnNewGameClicked()
    {
        if (saveSummary != null && saveSummary.HasSaveData)
        {
            ShowNewGameConfirm();
            return;
        }

        StartNewGame();
    }

    private void ShowNewGameConfirm()
    {
        SetLabel(newGameConfirmTitle, "기존 진행 데이터가 있습니다.");
        SetLabel(newGameConfirmMessage, "새 게임을 시작하면 기존 진행 상황이 초기화됩니다.");

        if (newGameConfirmOverlay != null)
            newGameConfirmOverlay.style.display = DisplayStyle.Flex;
    }

    private void HideNewGameConfirm()
    {
        if (newGameConfirmOverlay != null)
            newGameConfirmOverlay.style.display = DisplayStyle.None;
    }

    private void OnNewGameConfirmOkClicked()
    {
        HideNewGameConfirm();
        StartNewGame();
    }

    private void StartNewGame()
    {
        if (lobbyFlowEntry != null)
            lobbyFlowEntry.OnNewGameButtonPressed();
    }

    private void OnContinueClicked()
    {
        if (saveSummary == null || !saveSummary.HasSaveData)
            return;

        if (lobbyFlowEntry != null)
            lobbyFlowEntry.OnContinueButtonPressed();
    }

    private void OnCreditsClicked()
    {
        if (lobbyFlowEntry != null)
            lobbyFlowEntry.OnCreditsButtonPressed();
    }

    private void OnSettingsClicked()
    {
        if (lobbyFlowEntry != null)
            lobbyFlowEntry.OnSettingsButtonPressed();
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
