using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public sealed class GameOverResultHUD : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private ResultFlowEntry resultFlowEntry;

    private Label survivedDayValueLabel;
    private Label reachedWaveValueLabel;
    private Label killedEnemyCountValueLabel;
    private Label lostUndeadCountValueLabel;
    private Label revivedUndeadCountValueLabel;
    private Label totalCoreDamageTakenValueLabel;
    private Label lastAttackerNameValueLabel;
    private Label lordEvaluationTitleLabel;
    private Label lordEvaluationDescriptionLabel;
    private Label lordEvaluationHintLabel;
    private Button mainMenuButton;
    private VisualElement gameOverRoot;
    private VisualElement documentRoot;
    private VisualElement gameOverLayout;
    private VisualElement gameOverHeader;
    private VisualElement gameOverBody;
    private VisualElement gameOverFooter;
    private bool isBound;

    private void Awake()
    {
        ResolveReferences();
        BindVisualElements();
        RefreshInitialVisibility();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindVisualElements();
        RegisterEvents();
        EventBus.Instance.Subscribe<RequestOpenResultEvent>(OnRequestOpenResult);
        RefreshInitialVisibility();
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<RequestOpenResultEvent>(OnRequestOpenResult);
        UnregisterEvents();
    }

    public void ApplyResult(GameOverResultData resultData)
    {
        if (resultData == null)
            resultData = CreateFallbackResultData();

        SetLabel(survivedDayValueLabel, $"{Mathf.Max(1, resultData.survivedDay)}\uC77C");
        SetLabel(reachedWaveValueLabel, Mathf.Max(1, resultData.reachedWave).ToString());
        SetLabel(killedEnemyCountValueLabel, Mathf.Max(0, resultData.killedEnemyCount).ToString());
        SetLabel(lostUndeadCountValueLabel, Mathf.Max(0, resultData.lostUndeadCount).ToString());
        SetLabel(revivedUndeadCountValueLabel, Mathf.Max(0, resultData.revivedUndeadCount).ToString());
        SetLabel(totalCoreDamageTakenValueLabel, Mathf.Max(0, resultData.totalCoreDamageTaken).ToString());
        SetLabel(lastAttackerNameValueLabel, ResolveText(resultData.lastAttackerName, "\uC54C \uC218 \uC5C6\uC74C"));
        SetLabel(lordEvaluationTitleLabel, ResolveText(resultData.lordEvaluationTitle, "\uC804\uD22C \uC885\uB8CC"));
        SetLabel(lordEvaluationDescriptionLabel, ResolveText(resultData.lordEvaluationDescription, "\uCF54\uC5B4\uAC00 \uD30C\uAD34\uB418\uC5C8\uC2B5\uB2C8\uB2E4."));
        SetLabel(lordEvaluationHintLabel, ResolveText(resultData.lordEvaluationHint, "\uB85C\uBE44\uC5D0\uC11C \uC804\uC5F4\uC744 \uC815\uBE44\uD558\uACE0 \uB2E4\uC74C \uBC29\uC5B4\uB97C \uC900\uBE44\uD558\uC138\uC694."));
    }

    private void ResolveReferences()
    {
        UIDocument localDocument = GetComponent<UIDocument>();
        if (uiDocument == null || (localDocument != null && uiDocument.gameObject != gameObject))
            uiDocument = localDocument;

        if (resultFlowEntry == null)
            resultFlowEntry = GetComponent<ResultFlowEntry>();
    }

    private void BindVisualElements()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[GameOverResultHUD] UIDocument is not assigned.");
            return;
        }

        documentRoot = uiDocument.rootVisualElement;
        if (documentRoot == null)
        {
            Debug.LogWarning("[GameOverResultHUD] UIDocument root is not ready.");
            return;
        }

        survivedDayValueLabel = documentRoot.Q<Label>("SurvivedDayValueLabel");
        reachedWaveValueLabel = documentRoot.Q<Label>("ReachedWaveValueLabel");
        killedEnemyCountValueLabel = documentRoot.Q<Label>("KilledEnemyCountValueLabel");
        lostUndeadCountValueLabel = documentRoot.Q<Label>("LostUndeadCountValueLabel");
        revivedUndeadCountValueLabel = documentRoot.Q<Label>("RevivedUndeadCountValueLabel");
        totalCoreDamageTakenValueLabel = documentRoot.Q<Label>("TotalCoreDamageTakenValueLabel");
        lastAttackerNameValueLabel = documentRoot.Q<Label>("LastAttackerNameValueLabel");
        lordEvaluationTitleLabel = documentRoot.Q<Label>("LordEvaluationTitleLabel");
        lordEvaluationDescriptionLabel = documentRoot.Q<Label>("LordEvaluationDescriptionLabel");
        lordEvaluationHintLabel = documentRoot.Q<Label>("LordEvaluationHintLabel");
        mainMenuButton = documentRoot.Q<Button>("MainMenuButton");
        gameOverRoot = documentRoot.Q<VisualElement>("GameOverRoot");
        gameOverLayout = documentRoot.Q<VisualElement>(className: "game-over-layout");
        gameOverHeader = documentRoot.Q<VisualElement>(className: "game-over-header");
        gameOverBody = documentRoot.Q<VisualElement>(className: "game-over-body");
        gameOverFooter = documentRoot.Q<VisualElement>(className: "game-over-footer");

        ApplyRuntimeLayout();
    }

    private void RegisterEvents()
    {
        if (isBound)
            return;

        if (mainMenuButton != null)
            mainMenuButton.clicked += OnMainMenuClicked;

        isBound = true;
    }

    private void UnregisterEvents()
    {
        if (!isBound)
            return;

        if (mainMenuButton != null)
            mainMenuButton.clicked -= OnMainMenuClicked;

        isBound = false;
    }

    private void OnMainMenuClicked()
    {
        Hide();

        if (resultFlowEntry != null)
            resultFlowEntry.OnContinueToLobbyButtonPressed();
        else
            EventBus.Instance.Publish(new RequestOpenLobbyEvent());
    }

    private void OnRequestOpenResult(RequestOpenResultEvent e)
    {
        ApplyResult(e != null ? e.ResultData : GameOverController.LastResultData);
        Show();
    }

    private void RefreshInitialVisibility()
    {
        if (GameOverController.LastResultData != null && IsResultScene())
        {
            ApplyResult(GameOverController.LastResultData);
            Show();
            return;
        }

        Hide();
    }

    private static bool IsResultScene()
    {
        return string.Equals(SceneManager.GetActiveScene().name, "Result", System.StringComparison.Ordinal);
    }

    private void Show()
    {
        ApplyRuntimeLayout();
        SetVisible(true);
    }

    private void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (gameOverRoot != null)
            gameOverRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ApplyRuntimeLayout()
    {
        ApplyAbsoluteFill(documentRoot);
        ApplyAbsoluteFill(gameOverRoot);
        ApplyAbsoluteFill(gameOverLayout);

        if (gameOverHeader != null)
        {
            gameOverHeader.style.position = Position.Absolute;
            gameOverHeader.style.left = 70;
            gameOverHeader.style.right = 70;
            gameOverHeader.style.top = 36;
            gameOverHeader.style.height = 250;
            gameOverHeader.style.alignItems = Align.Center;
        }

        if (gameOverBody != null)
        {
            gameOverBody.style.position = Position.Absolute;
            gameOverBody.style.left = 0;
            gameOverBody.style.right = 0;
            gameOverBody.style.top = 286;
            gameOverBody.style.bottom = 150;
            gameOverBody.style.flexDirection = FlexDirection.Row;
            gameOverBody.style.justifyContent = Justify.SpaceBetween;
            gameOverBody.style.alignItems = Align.FlexStart;
            gameOverBody.style.paddingLeft = 110;
            gameOverBody.style.paddingRight = 110;
        }

        if (gameOverFooter != null)
        {
            gameOverFooter.style.position = Position.Absolute;
            gameOverFooter.style.left = 70;
            gameOverFooter.style.right = 70;
            gameOverFooter.style.bottom = 34;
            gameOverFooter.style.height = 106;
            gameOverFooter.style.flexDirection = FlexDirection.Row;
            gameOverFooter.style.justifyContent = Justify.Center;
            gameOverFooter.style.alignItems = Align.Center;
        }

        if (documentRoot == null)
            return;

        documentRoot.Query<VisualElement>(className: "result-panel-wrap").ForEach(panel =>
        {
            panel.style.position = Position.Relative;
            panel.style.width = 520;
            panel.style.height = 649;
        });
    }

    private static void ApplyAbsoluteFill(VisualElement element)
    {
        if (element == null)
            return;

        element.style.position = Position.Absolute;
        element.style.left = 0;
        element.style.right = 0;
        element.style.top = 0;
        element.style.bottom = 0;
    }

    private static GameOverResultData CreateFallbackResultData()
    {
        return new GameOverResultData
        {
            survivedDay = 1,
            reachedWave = 1,
            lastAttackerName = "\uC54C \uC218 \uC5C6\uC74C",
            lordEvaluationTitle = "\uC804\uD22C \uC885\uB8CC",
            lordEvaluationDescription = "\uCF54\uC5B4\uAC00 \uD30C\uAD34\uB418\uC5C8\uC2B5\uB2C8\uB2E4.",
            lordEvaluationHint = "\uB85C\uBE44\uC5D0\uC11C \uC804\uC5F4\uC744 \uC815\uBE44\uD558\uACE0 \uB2E4\uC74C \uBC29\uC5B4\uB97C \uC900\uBE44\uD558\uC138\uC694."
        };
    }

    private static void SetLabel(Label label, string value)
    {
        if (label != null)
            label.text = value;
    }

    private static string ResolveText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
