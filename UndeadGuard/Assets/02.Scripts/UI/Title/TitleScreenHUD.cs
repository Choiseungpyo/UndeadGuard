using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class TitleScreenHUD : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private VisualTreeAsset titleDocumentAsset;

    [Header("Sprites")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite logoSprite;
    [SerializeField] private Sprite pressAnyKeyPanelSprite;

    [Header("Text")]
    [SerializeField] private string pressAnyKeyText = "PRESS ANY KEY";

    [Header("Blink")]
    [SerializeField] private bool blinkPressAnyKeyText = true;
    [SerializeField] private float blinkSpeed = 1.1f;
    [SerializeField] private float minOpacity = 0.3f;
    [SerializeField] private float maxOpacity = 1f;

    private Label pressAnyKeyLabel;

    private void Awake()
    {
        EnsureDocument();
        AssignFallbackSpritesIfNeeded();
        BuildAndBind();
    }

    private void OnEnable()
    {
        EnsureDocument();
        AssignFallbackSpritesIfNeeded();
        BuildAndBind();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AssignFallbackSpritesIfNeeded();
        if (isActiveAndEnabled)
            BuildAndBind();
    }
#endif

    private void Update()
    {
        if (!blinkPressAnyKeyText || pressAnyKeyLabel == null)
            return;

        float cycle = Mathf.PingPong(Time.unscaledTime * Mathf.Max(0.01f, blinkSpeed), 1f);
        pressAnyKeyLabel.style.opacity = Mathf.Lerp(minOpacity, maxOpacity, cycle);
    }

    private void EnsureDocument()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
            uiDocument = gameObject.AddComponent<UIDocument>();

        if (panelSettings != null)
            uiDocument.panelSettings = panelSettings;

        if (titleDocumentAsset != null)
            uiDocument.visualTreeAsset = titleDocumentAsset;
    }

    private void BuildAndBind()
    {
        if (uiDocument == null)
            return;

        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
            return;

        VisualElement background = root.Q<VisualElement>("BackgroundImage");
        VisualElement logo = root.Q<VisualElement>("LogoImage");
        VisualElement panel = root.Q<VisualElement>("PressAnyKeyPanel");
        pressAnyKeyLabel = root.Q<Label>("PressAnyKeyText");

        if (background != null && backgroundSprite != null)
            background.style.backgroundImage = new StyleBackground(backgroundSprite);

        if (logo != null && logoSprite != null)
            logo.style.backgroundImage = new StyleBackground(logoSprite);

        if (panel != null && pressAnyKeyPanelSprite != null)
            panel.style.backgroundImage = new StyleBackground(pressAnyKeyPanelSprite);

        if (pressAnyKeyLabel != null)
            pressAnyKeyLabel.text = pressAnyKeyText;
    }

    private void AssignFallbackSpritesIfNeeded()
    {
#if UNITY_EDITOR
        if (backgroundSprite == null)
            backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/09.Download/UI/Title/Title.png");

        if (logoSprite == null)
            logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/09.Download/UI/Title/Logo.png");

        if (pressAnyKeyPanelSprite == null)
            pressAnyKeyPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/09.Download/UI/Title/PressAnyKeyButton.png");
#endif
    }
}
