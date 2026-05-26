using UnityEngine;
using UnityEngine.UIElements;

public class TutorialHUDController : Singleton<TutorialHUDController>
{
    [SerializeField] private UIDocument uiDocument;

    [Header("UXML Names")]
    [SerializeField] private string rootName = "TutorialRoot";
    [SerializeField] private string messageLabelName = "TutorialMessageLabel";
    [SerializeField] private string nextButtonName = "TutorialNextButton";
    [SerializeField] private string dimOverlayName = "TutorialDimOverlay";
    [SerializeField] private string targetFrameName = "TutorialTargetFrame";

    [Header("Target Names")]
    [SerializeField] private string phaseEndButtonName = "PhaseEndButton";
    [SerializeField] private string firstActionButtonName = "ActionButton0";

    [Header("Highlight")]
    [SerializeField] private float uiHighlightPadding = 8f;
    [SerializeField] private float worldHighlightPadding = 14f;
    [SerializeField] private float minWorldHighlightSize = 56f;

    private VisualElement root;
    private Label messageLabel;
    private Button nextButton;
    private VisualElement dimOverlay;
    private VisualElement targetFrame;
    private TutorialStep currentStep = TutorialStep.None;

    protected override void Awake()
    {
        base.Awake();

        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
            uiDocument = GetComponentInParent<UIDocument>();
    }

    private void OnEnable()
    {
        BindDocument();

        EventBus.Instance.Subscribe<TutorialStepChangedEvent>(OnTutorialStepChanged);
        Hide();
    }

    private void OnDisable()
    {
        if (nextButton != null)
            nextButton.UnregisterCallback<ClickEvent>(OnNextClicked);

        if (uiDocument != null)
        {
            if (BattleInputGuard.TryGetExisting(out var guard))
                guard.UnregisterDocument(uiDocument);
        }

        EventBus.Instance.Unsubscribe<TutorialStepChangedEvent>(OnTutorialStepChanged);
    }

    private void BindDocument()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[TutorialHUDController] UIDocument is not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement.Q<VisualElement>(rootName);
        if (root == null)
            CreateRuntimeHud(uiDocument.rootVisualElement);

        messageLabel = uiDocument.rootVisualElement.Q<Label>(messageLabelName);
        nextButton = uiDocument.rootVisualElement.Q<Button>(nextButtonName);
        dimOverlay = uiDocument.rootVisualElement.Q<VisualElement>(dimOverlayName);
        targetFrame = uiDocument.rootVisualElement.Q<VisualElement>(targetFrameName);

        ConfigurePicking();

        if (nextButton != null)
        {
            nextButton.UnregisterCallback<ClickEvent>(OnNextClicked);
            nextButton.RegisterCallback<ClickEvent>(OnNextClicked);
        }

        if (BattleInputGuard.Instance != null)
            BattleInputGuard.Instance.RegisterDocument(uiDocument);
    }

    private void OnTutorialStepChanged(TutorialStepChangedEvent e)
    {
        currentStep = e.Step;

        if (currentStep == TutorialStep.None)
        {
            Hide();
            return;
        }

        Show(e.Message);
        bool usesNextButton = currentStep == TutorialStep.ExplainEnemyDirection
            || currentStep == TutorialStep.ExplainCore
            || currentStep == TutorialStep.ExplainVictory;

        if (nextButton != null)
        {
            nextButton.style.display = usesNextButton ? DisplayStyle.Flex : DisplayStyle.None;
            nextButton.text = currentStep == TutorialStep.ExplainVictory ? "\uD655\uC778" : "\uB2E4\uC74C";
        }

        if (dimOverlay != null)
            dimOverlay.style.display = usesNextButton ? DisplayStyle.Flex : DisplayStyle.None;

        UpdateMessagePanelPlacement(currentStep);
        UpdateTargetHighlight(e);
    }

    private void Show(string message)
    {
        if (root != null)
            root.style.display = DisplayStyle.Flex;

        if (messageLabel != null)
            messageLabel.text = message;
    }

    private void Hide()
    {
        if (root != null)
            root.style.display = DisplayStyle.None;
    }

    private void OnNextClicked(ClickEvent e)
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.ContinueCurrentStep();
    }

    private void CreateRuntimeHud(VisualElement documentRoot)
    {
        if (documentRoot == null)
            return;

        root = new VisualElement { name = rootName };
        root.pickingMode = PickingMode.Ignore;
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.top = 0;
        root.style.right = 0;
        root.style.bottom = 0;
        root.style.display = DisplayStyle.None;

        dimOverlay = new VisualElement { name = dimOverlayName };
        dimOverlay.pickingMode = PickingMode.Ignore;
        dimOverlay.style.position = Position.Absolute;
        dimOverlay.style.left = 0;
        dimOverlay.style.top = 0;
        dimOverlay.style.right = 0;
        dimOverlay.style.bottom = 0;
        dimOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.36f);

        targetFrame = new VisualElement { name = targetFrameName };
        targetFrame.pickingMode = PickingMode.Ignore;
        targetFrame.style.position = Position.Absolute;
        targetFrame.style.borderTopWidth = 4;
        targetFrame.style.borderRightWidth = 4;
        targetFrame.style.borderBottomWidth = 4;
        targetFrame.style.borderLeftWidth = 4;
        targetFrame.style.borderTopColor = new Color(0.93f, 0.83f, 0.35f, 1f);
        targetFrame.style.borderRightColor = new Color(0.93f, 0.83f, 0.35f, 1f);
        targetFrame.style.borderBottomColor = new Color(0.93f, 0.83f, 0.35f, 1f);
        targetFrame.style.borderLeftColor = new Color(0.93f, 0.83f, 0.35f, 1f);
        targetFrame.style.backgroundColor = new Color(0.93f, 0.83f, 0.35f, 0.08f);

        VisualElement messagePanel = new VisualElement { name = "TutorialMessagePanel" };
        messagePanel.pickingMode = PickingMode.Position;
        messagePanel.style.position = Position.Absolute;
        messagePanel.style.left = 280;
        messagePanel.style.right = 280;
        messagePanel.style.bottom = 32;
        messagePanel.style.minHeight = 96;
        messagePanel.style.flexDirection = FlexDirection.Row;
        messagePanel.style.alignItems = Align.Center;
        messagePanel.style.justifyContent = Justify.SpaceBetween;
        messagePanel.style.paddingLeft = 24;
        messagePanel.style.paddingRight = 18;
        messagePanel.style.paddingTop = 14;
        messagePanel.style.paddingBottom = 14;
        messagePanel.style.backgroundColor = new Color(0.05f, 0.04f, 0.04f, 0.92f);
        SetBorder(messagePanel, 2f, new Color(0.8f, 0.74f, 0.52f, 1f));

        messageLabel = new Label { name = messageLabelName };
        messageLabel.style.flexGrow = 1;
        messageLabel.style.whiteSpace = WhiteSpace.Normal;
        messageLabel.style.fontSize = 20;
        messageLabel.style.color = new Color(0.96f, 0.92f, 0.81f, 1f);

        nextButton = new Button { name = nextButtonName, text = "\uB2E4\uC74C" };
        nextButton.pickingMode = PickingMode.Position;
        nextButton.style.width = 92;
        nextButton.style.height = 54;
        nextButton.style.marginLeft = 20;
        nextButton.style.fontSize = 18;
        nextButton.style.color = new Color(0.1f, 0.08f, 0.07f, 1f);
        nextButton.style.backgroundColor = new Color(0.87f, 0.78f, 0.49f, 1f);

        messagePanel.Add(messageLabel);
        messagePanel.Add(nextButton);
        root.Add(dimOverlay);
        root.Add(targetFrame);
        root.Add(messagePanel);
        documentRoot.Add(root);
    }

    private void ConfigurePicking()
    {
        if (root != null)
            root.pickingMode = PickingMode.Ignore;

        if (dimOverlay != null)
            dimOverlay.pickingMode = PickingMode.Ignore;

        if (targetFrame != null)
            targetFrame.pickingMode = PickingMode.Ignore;

        VisualElement messagePanel = uiDocument != null
            ? uiDocument.rootVisualElement.Q<VisualElement>("TutorialMessagePanel")
            : null;

        if (messagePanel != null)
            messagePanel.pickingMode = PickingMode.Position;

        if (nextButton != null)
            nextButton.pickingMode = PickingMode.Position;
    }

    private void UpdateMessagePanelPlacement(TutorialStep step)
    {
        VisualElement messagePanel = uiDocument != null
            ? uiDocument.rootVisualElement.Q<VisualElement>("TutorialMessagePanel")
            : null;

        if (messagePanel == null)
            return;

        bool placeTop = step == TutorialStep.PressAttackButton
            || step == TutorialStep.AttackEnemy
            || step == TutorialStep.FreePlay;

        if (placeTop)
        {
            messagePanel.style.top = 32;
            messagePanel.style.bottom = StyleKeyword.Auto;
        }
        else
        {
            messagePanel.style.top = StyleKeyword.Auto;
            messagePanel.style.bottom = 32;
        }
    }

    private void UpdateTargetHighlight(TutorialStepChangedEvent e)
    {
        if (TryGetUiTargetRect(currentStep, out Rect uiRect))
        {
            SetHighlightVisible(true, ExpandRect(uiRect, uiHighlightPadding));
            return;
        }

        if (ShouldHighlightTargetUnit(currentStep) && e.TargetUnit != null && TryGetUnitRect(e.TargetUnit, out Rect unitRect))
        {
            SetHighlightVisible(true, ExpandRect(unitRect, worldHighlightPadding));
            return;
        }

        if (e.HasTargetGridPosition && TryGetTileRect(e.TargetGridPosition, out Rect tileRect))
        {
            SetHighlightVisible(true, ExpandRect(tileRect, worldHighlightPadding));
            return;
        }

        SetHighlightVisible(false, Rect.zero);
    }

    private bool TryGetUiTargetRect(TutorialStep step, out Rect rect)
    {
        rect = Rect.zero;

        string targetName = null;
        if (step == TutorialStep.PressAttackButton)
            targetName = firstActionButtonName;
        else if (step == TutorialStep.EndTurn || step == TutorialStep.StartBattle)
            targetName = phaseEndButtonName;

        if (string.IsNullOrEmpty(targetName))
            return false;

        return TryFindVisualElementRect(targetName, out rect);
    }

    private bool TryFindVisualElementRect(string elementName, out Rect rect)
    {
        rect = Rect.zero;

        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];
            if (document == null || document.rootVisualElement == null)
                continue;

            VisualElement element = document.rootVisualElement.Q<VisualElement>(elementName);
            if (!IsVisibleElement(element))
                continue;

            Rect worldBound = element.worldBound;
            if (worldBound.width <= 0f || worldBound.height <= 0f)
                continue;

            rect = worldBound;
            return true;
        }

        return false;
    }

    private bool TryGetTileRect(Vector2Int gridPosition, out Rect rect)
    {
        rect = Rect.zero;
        if (GridManager.Instance == null)
            return false;

        float spacing = GridManager.TileSpacing;
        float x = gridPosition.x * spacing;
        float z = gridPosition.y * spacing;
        Vector3[] corners =
        {
            new Vector3(x, 0f, z),
            new Vector3(x + spacing, 0f, z),
            new Vector3(x, 0f, z + spacing),
            new Vector3(x + spacing, 0f, z + spacing)
        };

        return TryGetProjectedRect(corners, out rect);
    }

    private bool TryGetUnitRect(UnitBase unit, out Rect rect)
    {
        rect = Rect.zero;
        if (unit == null)
            return false;

        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(unit.transform.position, Vector3.one * GridManager.TileSpacing);
        bool hasRenderer = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasRenderer)
            {
                bounds = renderer.bounds;
                hasRenderer = true;
            }
            else
                bounds.Encapsulate(renderer.bounds);
        }

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, max.y, max.z)
        };

        if (!TryGetProjectedRect(corners, out rect))
            return false;

        if (rect.width < minWorldHighlightSize)
            rect = ExpandWidth(rect, minWorldHighlightSize - rect.width);

        if (rect.height < minWorldHighlightSize)
            rect = ExpandHeight(rect, minWorldHighlightSize - rect.height);

        return true;
    }

    private bool TryGetProjectedRect(Vector3[] worldPoints, out Rect rect)
    {
        rect = Rect.zero;
        if (Camera.main == null || root == null || root.panel == null || worldPoints == null || worldPoints.Length == 0)
            return false;

        bool hasPoint = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;

        for (int i = 0; i < worldPoints.Length; i++)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPoints[i]);
            if (screenPosition.z < 0f)
                continue;

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
            if (!hasPoint)
            {
                min = panelPosition;
                max = panelPosition;
                hasPoint = true;
            }
            else
            {
                min = Vector2.Min(min, panelPosition);
                max = Vector2.Max(max, panelPosition);
            }
        }

        if (!hasPoint)
            return false;

        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return rect.width > 0f && rect.height > 0f;
    }

    private void SetHighlightVisible(bool visible, Rect rect)
    {
        if (targetFrame != null)
        {
            targetFrame.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            targetFrame.style.left = rect.x;
            targetFrame.style.top = rect.y;
            targetFrame.style.width = rect.width;
            targetFrame.style.height = rect.height;
        }

    }

    private static bool ShouldHighlightTargetUnit(TutorialStep step)
    {
        return step == TutorialStep.RelocateUndead;
    }

    private static Rect ExpandRect(Rect rect, float padding)
    {
        return Rect.MinMaxRect(
            rect.xMin - padding,
            rect.yMin - padding,
            rect.xMax + padding,
            rect.yMax + padding);
    }

    private static Rect ExpandWidth(Rect rect, float extraWidth)
    {
        float half = extraWidth * 0.5f;
        return Rect.MinMaxRect(rect.xMin - half, rect.yMin, rect.xMax + half, rect.yMax);
    }

    private static Rect ExpandHeight(Rect rect, float extraHeight)
    {
        float half = extraHeight * 0.5f;
        return Rect.MinMaxRect(rect.xMin, rect.yMin - half, rect.xMax, rect.yMax + half);
    }

    private static bool IsVisibleElement(VisualElement element)
    {
        if (element == null)
            return false;

        return element.resolvedStyle.display != DisplayStyle.None
            && element.resolvedStyle.visibility == Visibility.Visible
            && element.resolvedStyle.opacity > 0f;
    }

    private static void SetBorder(VisualElement element, float width, Color color)
    {
        element.style.borderTopWidth = width;
        element.style.borderRightWidth = width;
        element.style.borderBottomWidth = width;
        element.style.borderLeftWidth = width;
        element.style.borderTopColor = color;
        element.style.borderRightColor = color;
        element.style.borderBottomColor = color;
        element.style.borderLeftColor = color;
    }
}
