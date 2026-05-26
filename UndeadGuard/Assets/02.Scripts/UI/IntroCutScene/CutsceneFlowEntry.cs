using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class CutsceneFlowEntry : MonoBehaviour
{
    [SerializeField] private AppFlowSceneConfig sceneConfig;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private VisualTreeAsset cutsceneDocumentAsset;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool autoFixVideoOutputTarget = true;
    [SerializeField] private float lobbyTransitionDelayAfterCutsceneEnd = 1.5f;
    [SerializeField] private bool useUnscaledTimeForDelay = true;
    [SerializeField, Min(0f)] private float prepareTimeout = 3f;

    [Header("Opening Fade")]
    [SerializeField] private bool playOpeningFade = true;
    [SerializeField, Min(0f)] private float openingFadeDuration = 1.5f;

    [Header("Subtitles")]
    [SerializeField] private CutsceneSubtitleData subtitleData;

    [Header("Skip Input")]
    [SerializeField] private bool allowSkipByInput = true;
    [SerializeField] private bool ignoreEscapeKey = false;

    private const string FadePanelName = "OpeningFadePanel";
    private const string SubtitleTextName = "SubtitleText";

    private bool isTransitioning;
    private bool hasFinishTriggered;
    private Coroutine playbackCoroutine;
    private Coroutine delayedTransitionCoroutine;
    private Coroutine openingFadeCoroutine;
    private Coroutine subtitleCoroutine;
    private VisualElement fadePanel;
    private Label subtitleLabel;
    private float fallbackPlaybackStartTime;
    private bool skipInputWasHeld;

    private void Awake()
    {
        EnsureVideoPlayerReference();
        AssignFallbackAssetsIfNeeded();
        EnsureDocument();
        BindUI();
        SetFadeAlpha(1f);
        SetSubtitleVisible(false);
    }

    private void OnEnable()
    {
        fallbackPlaybackStartTime = Time.unscaledTime;
        EnsureVideoPlayerReference();
        AssignFallbackAssetsIfNeeded();
        EnsureDocument();
        BindUI();
        SetFadeAlpha(1f);
        SetSubtitleVisible(false);

        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached -= OnVideoLoopPointReached;
        videoPlayer.loopPointReached += OnVideoLoopPointReached;

        videoPlayer.errorReceived -= OnVideoErrorReceived;
        videoPlayer.errorReceived += OnVideoErrorReceived;

        if (autoFixVideoOutputTarget)
            ConfigureVideoOutputTargetIfNeeded();

        if (playOnEnable)
            playbackCoroutine = StartCoroutine(PlayCutsceneRoutine());
    }

    private void OnDisable()
    {
        StopPlayback();
        StopOpeningFade();
        StopSubtitles();
        CancelDelayedTransition();

        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached -= OnVideoLoopPointReached;
        videoPlayer.errorReceived -= OnVideoErrorReceived;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AssignFallbackAssetsIfNeeded();
    }
#endif

    private void Update()
    {
        if (!allowSkipByInput || isTransitioning)
            return;

        if (!IsSkipInputDetected())
            return;

        OnSkipRequested();
    }

    public void OnCutsceneFinished()
    {
        if (isTransitioning || hasFinishTriggered)
            return;

        hasFinishTriggered = true;

        float delay = Mathf.Max(0f, lobbyTransitionDelayAfterCutsceneEnd);
        if (delay <= 0f)
        {
            TransitionToLobby();
            return;
        }

        delayedTransitionCoroutine = StartCoroutine(DelayedLobbyTransitionRoutine(delay));
    }

    public void OnSkipRequested()
    {
        if (isTransitioning)
            return;

        StopOpeningFade();
        SetFadeAlpha(1f);
        StopSubtitles();
        StopPlayback();
        CancelDelayedTransition();
        if (videoPlayer != null)
            videoPlayer.Stop();

        TransitionToLobby();
    }

    private IEnumerator PlayCutsceneRoutine()
    {
        if (videoPlayer == null)
            yield break;

        videoPlayer.playOnAwake = false;

        if (!videoPlayer.isPrepared)
        {
            videoPlayer.Prepare();

            float elapsed = 0f;
            float timeout = Mathf.Max(0f, prepareTimeout);
            while (!videoPlayer.isPrepared && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        fallbackPlaybackStartTime = Time.unscaledTime;
        videoPlayer.Play();
        StartSubtitles();
        StartOpeningFade();
        playbackCoroutine = null;
    }

    private void StopPlayback()
    {
        if (playbackCoroutine == null)
            return;

        StopCoroutine(playbackCoroutine);
        playbackCoroutine = null;
    }

    private void OnVideoLoopPointReached(VideoPlayer source)
    {
        OnCutsceneFinished();
    }

    private void OnVideoErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogWarning($"[CutsceneFlowEntry] VideoPlayer error: {message}");
    }

    private void EnsureVideoPlayerReference()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
    }

    private void EnsureDocument()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
            uiDocument = gameObject.AddComponent<UIDocument>();

        if (panelSettings != null)
            uiDocument.panelSettings = panelSettings;

        if (cutsceneDocumentAsset != null)
            uiDocument.visualTreeAsset = cutsceneDocumentAsset;
    }

    private void BindUI()
    {
        if (uiDocument == null)
            return;

        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
            return;

        fadePanel = root.Q<VisualElement>(FadePanelName);
        subtitleLabel = root.Q<Label>(SubtitleTextName);
    }

    private void StartOpeningFade()
    {
        StopOpeningFade();

        if (!playOpeningFade)
        {
            SetFadeAlpha(0f);
            return;
        }

        openingFadeCoroutine = StartCoroutine(OpeningFadeRoutine());
    }

    private void StopOpeningFade()
    {
        if (openingFadeCoroutine == null)
            return;

        StopCoroutine(openingFadeCoroutine);
        openingFadeCoroutine = null;
    }

    private IEnumerator OpeningFadeRoutine()
    {
        float duration = Mathf.Max(0f, openingFadeDuration);
        SetFadeAlpha(1f);

        if (duration <= 0f)
        {
            SetFadeAlpha(0f);
            openingFadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            SetFadeAlpha(alpha);
            yield return null;
        }

        SetFadeAlpha(0f);
        openingFadeCoroutine = null;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadePanel == null)
            return;

        float clampedAlpha = Mathf.Clamp01(alpha);
        fadePanel.style.opacity = clampedAlpha;
        fadePanel.pickingMode = clampedAlpha > 0f ? PickingMode.Position : PickingMode.Ignore;
    }

    private void StartSubtitles()
    {
        StopSubtitles();

        if (subtitleData == null || subtitleLabel == null)
        {
            SetSubtitleVisible(false);
            return;
        }

        subtitleCoroutine = StartCoroutine(SubtitleRoutine());
    }

    private void StopSubtitles()
    {
        if (subtitleCoroutine != null)
        {
            StopCoroutine(subtitleCoroutine);
            subtitleCoroutine = null;
        }

        SetSubtitleVisible(false);
    }

    private IEnumerator SubtitleRoutine()
    {
        string currentText = null;

        while (!isTransitioning)
        {
            float playbackTime = GetPlaybackTime();
            string nextText = ResolveSubtitleText(playbackTime);

            if (!string.Equals(currentText, nextText, System.StringComparison.Ordinal))
            {
                currentText = nextText;
                SetSubtitleText(currentText);
            }

            yield return null;
        }

        subtitleCoroutine = null;
    }

    private float GetPlaybackTime()
    {
        if (videoPlayer != null && (videoPlayer.isPlaying || videoPlayer.isPrepared))
            return Mathf.Max(0f, (float)videoPlayer.time);

        return Mathf.Max(0f, Time.unscaledTime - fallbackPlaybackStartTime);
    }

    private string ResolveSubtitleText(float playbackTime)
    {
        IReadOnlyList<CutsceneSubtitleEntry> entries = subtitleData.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            CutsceneSubtitleEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.text))
                continue;

            if (playbackTime >= entry.startTime && playbackTime < entry.EndTime)
                return entry.text;
        }

        return null;
    }

    private void SetSubtitleText(string text)
    {
        bool visible = !string.IsNullOrEmpty(text);
        if (subtitleLabel != null)
            subtitleLabel.text = visible ? text : string.Empty;

        SetSubtitleVisible(visible);
    }

    private void SetSubtitleVisible(bool visible)
    {
        if (subtitleLabel == null)
            return;

        subtitleLabel.style.opacity = visible ? 1f : 0f;
        if (!visible)
            subtitleLabel.text = string.Empty;
    }

    private void ConfigureVideoOutputTargetIfNeeded()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.clip == null && string.IsNullOrWhiteSpace(videoPlayer.url))
        {
            Debug.LogWarning("[CutsceneFlowEntry] Video clip/url is not assigned.");
            return;
        }

        if (videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource && videoPlayer.GetTargetAudioSource(0) == null)
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            targetCamera = FindFirstObjectByType<Camera>();

        if (targetCamera == null)
        {
            Debug.LogWarning("[CutsceneFlowEntry] No camera found to render cutscene video.");
            return;
        }

        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = targetCamera;
        videoPlayer.targetCameraAlpha = 1f;
    }

    private void TransitionToLobby()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;
        string sceneName = sceneConfig != null ? sceneConfig.LobbySceneName : string.Empty;
        EventBus.Instance.Publish(new RequestOpenLobbyEvent
        {
            SceneName = sceneName
        });

        if (GameManager.Instance == null)
        {
            LoadLobbySceneDirectly(sceneName);
            return;
        }

        if (!GameManager.Instance.IsTransitioning)
            LoadLobbySceneDirectly(sceneName);
    }

    private void LoadLobbySceneDirectly(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return;
        }

        Debug.LogWarning($"[CutsceneFlowEntry] Lobby scene name is empty or not loadable. sceneName : {sceneName}");
        isTransitioning = false;
    }

    private IEnumerator DelayedLobbyTransitionRoutine(float delaySeconds)
    {
        if (useUnscaledTimeForDelay)
            yield return new WaitForSecondsRealtime(delaySeconds);
        else
            yield return new WaitForSeconds(delaySeconds);

        delayedTransitionCoroutine = null;
        TransitionToLobby();
    }

    private void CancelDelayedTransition()
    {
        if (delayedTransitionCoroutine == null)
            return;

        StopCoroutine(delayedTransitionCoroutine);
        delayedTransitionCoroutine = null;
    }

    private bool IsSkipInputDetected()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return false;

        bool escapeDown = !ignoreEscapeKey && Keyboard.current.escapeKey.isPressed;
        bool spaceDown = Keyboard.current.spaceKey.isPressed;
        bool skipDown = escapeDown || spaceDown;
        bool skipPressed = skipDown && !skipInputWasHeld;
        skipInputWasHeld = skipDown;

        return skipPressed || (!ignoreEscapeKey && Keyboard.current.escapeKey.wasPressedThisFrame) || Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        bool escapePressed = !ignoreEscapeKey && Input.GetKeyDown(KeyCode.Escape);
        bool spacePressed = Input.GetKeyDown(KeyCode.Space);

        return escapePressed || spacePressed;
#endif
    }

    private void AssignFallbackAssetsIfNeeded()
    {
#if UNITY_EDITOR
        if (panelSettings == null)
            panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset");

        if (cutsceneDocumentAsset == null)
            cutsceneDocumentAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/07.UI/IntroCutSceneHUD.uxml");
#endif
    }
}
