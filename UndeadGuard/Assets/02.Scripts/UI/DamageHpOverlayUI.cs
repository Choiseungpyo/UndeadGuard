using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DamageHpOverlayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument targetDocument;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private VisualTreeAsset hpBarTemplateAsset;

    [Header("Template Query")]
    [SerializeField] private string hpFillElementName = "HpFill";
    [SerializeField] private string hpLabelElementName = "HpLabel";

    [Header("Behavior")]
    [SerializeField] private float worldOffset = 0.25f;
    [SerializeField] private float pixelOffset = 18f;
    [SerializeField] private float visibleDuration = 2f;
    [SerializeField] private float deadHoldDuration = 0.5f;
    [SerializeField] private float hpChangeAnimDuration = 0.2f;

    [Header("Damage Text")]
    [SerializeField] private bool showWhenNoHpChange = true;
    [SerializeField] private float damageTextDelay = 0.2f;
    [SerializeField] private float damageTextHoldDuration = 0.24f;
    [SerializeField] private float damageTextMoveDuration = 0.6f;
    [SerializeField] private float damageTextRise = 18f;
    [SerializeField] private float damageTextBaseTop = -14f;
    [SerializeField] private int damageTextFontSize = 11;
    [SerializeField] private Color damageTextColor = new Color(1f, 0.35f, 0.25f, 1f);

    [Header("Template Fallback Size")]
    [SerializeField] private float fallbackBarWidth = 72f;
    [SerializeField] private float fallbackBarHeight = 20f;

    [Header("Style")]
    [SerializeField] private Color highHpColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color midHpColor = new Color(0.9f, 0.75f, 0.15f, 1f);
    [SerializeField] private Color lowHpColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private bool useDynamicFillColor = true;

    private const string OverlayRootName = "DamageHpOverlayRoot";

    private readonly Dictionary<int, HpBarEntry> entries = new Dictionary<int, HpBarEntry>();
    private readonly List<int> removeBuffer = new List<int>();

    private VisualElement overlayRoot;
    private bool templateWarningLogged;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<DamageTakenEvent>(OnDamageTaken);
        EventBus.Instance.Subscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Instance.Subscribe<UnitRevivedEvent>(OnUnitRevived);
        EventBus.Instance.Subscribe<CoreDestroyedEvent>(OnCoreDestroyed);

        TryEnsureOverlayRoot();
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<DamageTakenEvent>(OnDamageTaken);
        EventBus.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Instance.Unsubscribe<UnitRevivedEvent>(OnUnitRevived);
        EventBus.Instance.Unsubscribe<CoreDestroyedEvent>(OnCoreDestroyed);
        ClearAllBars();
    }

    private void LateUpdate()
    {
        if (entries.Count == 0)
            return;

        if (!TryEnsureOverlayRoot())
            return;

        Camera cam = ResolveCamera();
        if (cam == null)
            return;

        float now = Time.unscaledTime;
        removeBuffer.Clear();

        foreach (var pair in entries)
        {
            HpBarEntry entry = pair.Value;
            if (entry == null || entry.Target == null)
            {
                removeBuffer.Add(pair.Key);
                continue;
            }

            UpdateHpAnimation(entry, now);
            UpdateDamageText(entry, now);

            if (!entry.Target.gameObject.activeInHierarchy)
            {
                SetVisible(entry, false);
                continue;
            }

            if (now > entry.VisibleUntil)
            {
                SetVisible(entry, false);
                continue;
            }

            bool placed = TryPlaceEntry(entry, cam);
            SetVisible(entry, placed);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            int key = removeBuffer[i];
            if (!entries.TryGetValue(key, out HpBarEntry entry))
                continue;

            if (entry.Root != null)
                entry.Root.RemoveFromHierarchy();

            entries.Remove(key);
        }
    }

    private void OnDamageTaken(DamageTakenEvent e)
    {
        if (e == null || e.TargetBehaviour == null)
            return;

        if (!showWhenNoHpChange && e.Damage <= 0)
            return;

        if (!TryEnsureOverlayRoot())
            return;

        float now = Time.unscaledTime;
        HpBarEntry entry = GetOrCreateEntry(e.TargetBehaviour);
        if (entry == null)
            return;

        entry.CurrentHp = Mathf.Max(0, e.CurrentHp);
        entry.MaxHp = Mathf.Max(1, e.MaxHp);

        int prevHp = Mathf.Clamp(entry.CurrentHp + Mathf.Max(0, e.Damage), 0, entry.MaxHp);
        float fromRatio = Mathf.Clamp01((float)prevHp / entry.MaxHp);
        float toRatio = Mathf.Clamp01((float)entry.CurrentHp / entry.MaxHp);

        BeginHpAnimation(entry, fromRatio, toRatio, now);
        UpdateEntryVisual(entry, entry.DisplayRatio);

        ShowDamageText(entry, Mathf.Max(0, e.Damage), now);

        entry.VisibleUntil = now + visibleDuration;
        SetVisible(entry, true);
    }

    private void OnUnitDied(UnitDiedEvent e)
    {
        if (e == null || e.Unit == null)
            return;

        if (entries.TryGetValue(e.Unit.GetInstanceID(), out HpBarEntry entry))
        {
            float now = Time.unscaledTime;
            entry.CurrentHp = 0;
            entry.MaxHp = Mathf.Max(1, entry.MaxHp);
            BeginHpAnimation(entry, entry.DisplayRatio, 0f, now);
            UpdateEntryVisual(entry, entry.DisplayRatio);
            entry.VisibleUntil = now + deadHoldDuration;
        }
    }

    private void OnUnitRevived(UnitRevivedEvent e)
    {
        if (e == null || e.Unit == null)
            return;

        if (entries.TryGetValue(e.Unit.GetInstanceID(), out HpBarEntry entry))
        {
            entry.CurrentHp = e.Unit.Stats.CurrentHp;
            entry.MaxHp = Mathf.Max(1, e.Unit.Stats.MaxHp);
            entry.DisplayRatio = 1f;
            entry.FromRatio = 1f;
            entry.ToRatio = 1f;
            entry.RatioAnimEndTime = 0f;
            UpdateEntryVisual(entry, entry.DisplayRatio);
            SetVisible(entry, false);
        }
    }

    private void OnCoreDestroyed(CoreDestroyedEvent e)
    {
        float now = Time.unscaledTime;

        foreach (var pair in entries)
        {
            HpBarEntry entry = pair.Value;
            if (entry == null || !(entry.Target is CoreHealth))
                continue;

            entry.CurrentHp = 0;
            entry.MaxHp = Mathf.Max(1, entry.MaxHp);
            BeginHpAnimation(entry, entry.DisplayRatio, 0f, now);
            UpdateEntryVisual(entry, entry.DisplayRatio);
            entry.VisibleUntil = now + deadHoldDuration;
        }
    }

    private HpBarEntry GetOrCreateEntry(MonoBehaviour target)
    {
        int key = target.GetInstanceID();
        if (entries.TryGetValue(key, out HpBarEntry existing))
            return existing;

        HpBarEntry created = CreateEntry(target);
        if (created == null)
            return null;

        entries[key] = created;
        return created;
    }

    private HpBarEntry CreateEntry(MonoBehaviour target)
    {
        if (!TryGetTemplateAsset(out VisualTreeAsset template))
            return null;

        VisualElement root = template.CloneTree();
        root.name = "DamageHpBar";
        root.pickingMode = PickingMode.Ignore;
        root.style.position = Position.Absolute;
        root.style.display = DisplayStyle.None;
        root.style.left = 0f;
        root.style.top = 0f;

        VisualElement fill = root.Q<VisualElement>(hpFillElementName);
        Label hpLabel = root.Q<Label>(hpLabelElementName);

        if (fill == null)
        {
            Debug.LogWarning($"DamageHpOverlayUI: cannot find fill element '{hpFillElementName}' in HP bar template.");
            return null;
        }

        fill.pickingMode = PickingMode.Ignore;

        if (hpLabel != null)
            hpLabel.pickingMode = PickingMode.Ignore;

        Label damageLabel = new Label();
        damageLabel.name = "DamageValueLabel";
        damageLabel.pickingMode = PickingMode.Ignore;
        damageLabel.style.position = Position.Absolute;
        damageLabel.style.left = 0f;
        damageLabel.style.right = 0f;
        damageLabel.style.top = damageTextBaseTop;
        damageLabel.style.color = damageTextColor;
        damageLabel.style.fontSize = damageTextFontSize;
        damageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        damageLabel.style.display = DisplayStyle.None;
        root.Add(damageLabel);

        overlayRoot.Add(root);

        var entry = new HpBarEntry
        {
            Target = target,
            Root = root,
            Fill = fill,
            HpLabel = hpLabel,
            DamageLabel = damageLabel,
            Colliders = target.GetComponentsInChildren<Collider>(false),
            Renderers = target.GetComponentsInChildren<Renderer>(false),
            CurrentHp = 1,
            MaxHp = 1,
            VisibleUntil = 0f,
            DisplayRatio = 1f,
            FromRatio = 1f,
            ToRatio = 1f,
            RatioAnimEndTime = 0f,
            DamageTextStartTime = 0f,
            DamageTextHoldEndTime = 0f,
            DamageTextEndTime = 0f,
            DamageTextBaseTop = damageTextBaseTop
        };

        UpdateEntryVisual(entry, entry.DisplayRatio);
        return entry;
    }

    private void BeginHpAnimation(HpBarEntry entry, float fromRatio, float toRatio, float now)
    {
        entry.FromRatio = Mathf.Clamp01(fromRatio);
        entry.ToRatio = Mathf.Clamp01(toRatio);
        entry.DisplayRatio = entry.FromRatio;
        entry.RatioAnimStartTime = now;
        entry.RatioAnimEndTime = now + Mathf.Max(0.01f, hpChangeAnimDuration);
    }

    private void UpdateHpAnimation(HpBarEntry entry, float now)
    {
        if (entry.RatioAnimEndTime <= entry.RatioAnimStartTime)
        {
            entry.DisplayRatio = entry.ToRatio;
            UpdateEntryVisual(entry, entry.DisplayRatio);
            return;
        }

        if (now >= entry.RatioAnimEndTime)
        {
            entry.DisplayRatio = entry.ToRatio;
            UpdateEntryVisual(entry, entry.DisplayRatio);
            return;
        }

        float t = Mathf.InverseLerp(entry.RatioAnimStartTime, entry.RatioAnimEndTime, now);
        float eased = 1f - ((1f - t) * (1f - t));
        entry.DisplayRatio = Mathf.Lerp(entry.FromRatio, entry.ToRatio, eased);
        UpdateEntryVisual(entry, entry.DisplayRatio);
    }
    private void ShowDamageText(HpBarEntry entry, int damage, float now)
    {
        if (entry.DamageLabel == null)
            return;

        float startTop = GetDamageTextBaseTop(entry);

        entry.DamageLabel.text = $"-{damage}";
        entry.DamageLabel.style.display = DisplayStyle.None;
        entry.DamageLabel.style.opacity = 1f;
        entry.DamageLabel.style.top = startTop;

        entry.DamageTextBaseTop = startTop;
        entry.DamageTextStartTime = now + Mathf.Max(0f, damageTextDelay);
        entry.DamageTextHoldEndTime = entry.DamageTextStartTime + Mathf.Max(0f, damageTextHoldDuration);
        entry.DamageTextEndTime = entry.DamageTextHoldEndTime + Mathf.Max(0.05f, damageTextMoveDuration);
    }

    private void UpdateDamageText(HpBarEntry entry, float now)
    {
        if (entry.DamageLabel == null)
            return;

        if (now < entry.DamageTextStartTime)
        {
            entry.DamageLabel.style.display = DisplayStyle.None;
            return;
        }

        if (now < entry.DamageTextHoldEndTime)
        {
            entry.DamageLabel.style.display = DisplayStyle.Flex;
            entry.DamageLabel.style.top = entry.DamageTextBaseTop;
            entry.DamageLabel.style.opacity = 1f;
            return;
        }

        if (now >= entry.DamageTextEndTime)
        {
            entry.DamageLabel.style.display = DisplayStyle.None;
            return;
        }

        float t = Mathf.InverseLerp(entry.DamageTextHoldEndTime, entry.DamageTextEndTime, now);
        float riseT = t * t;
        float top = entry.DamageTextBaseTop - (damageTextRise * riseT);

        entry.DamageLabel.style.display = DisplayStyle.Flex;
        entry.DamageLabel.style.top = top;
        entry.DamageLabel.style.opacity = 1f - t;
    }

    private float GetDamageTextBaseTop(HpBarEntry entry)
    {
        float barHalfHeight = GetResolvedHeight(entry.Root, fallbackBarHeight) * 0.5f;
        return damageTextBaseTop - barHalfHeight;
    }

    private void UpdateEntryVisual(HpBarEntry entry, float ratio)
    {
        if (entry == null || entry.Fill == null)
            return;

        float clamped = Mathf.Clamp01(ratio);
        entry.Fill.style.width = Length.Percent(clamped * 100f);

        if (useDynamicFillColor)
            entry.Fill.style.backgroundColor = GetHpColor(clamped);

        if (entry.HpLabel != null)
            entry.HpLabel.text = $"{entry.CurrentHp} / {entry.MaxHp}";
    }

    private bool TryPlaceEntry(HpBarEntry entry, Camera cam)
    {
        if (overlayRoot == null || overlayRoot.panel == null)
            return false;

        if (!TryGetWorldAnchor(entry, out Vector3 anchor))
            return false;

        Vector3 screenPos = cam.WorldToScreenPoint(anchor);
        if (screenPos.z <= 0f)
            return false;

        Rect cameraRect = cam.pixelRect;
        if (cameraRect.width <= 0f || cameraRect.height <= 0f)
            return false;

        float nx = Mathf.InverseLerp(cameraRect.xMin, cameraRect.xMax, screenPos.x);
        float ny = Mathf.InverseLerp(cameraRect.yMin, cameraRect.yMax, screenPos.y);

        if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
            return false;

        float panelWidth = overlayRoot.resolvedStyle.width;
        float panelHeight = overlayRoot.resolvedStyle.height;
        if (panelWidth <= 0f || panelHeight <= 0f)
            return false;

        float barWidth = GetResolvedWidth(entry.Root, fallbackBarWidth);
        float barHeight = GetResolvedHeight(entry.Root, fallbackBarHeight);

        float x = (nx * panelWidth) - (barWidth * 0.5f);
        float y = ((1f - ny) * panelHeight) - pixelOffset;

        x = Mathf.Clamp(x, 0f, Mathf.Max(0f, panelWidth - barWidth));
        y = Mathf.Clamp(y, 0f, Mathf.Max(0f, panelHeight - barHeight));

        entry.Root.style.left = x;
        entry.Root.style.top = y;
        return true;
    }

    private bool TryGetWorldAnchor(HpBarEntry entry, out Vector3 anchor)
    {
        if (entry == null || entry.Target == null)
        {
            anchor = default;
            return false;
        }

        if (TryGetBounds(entry, out Bounds bounds))
        {
            anchor = new Vector3(bounds.center.x, bounds.max.y + worldOffset, bounds.center.z);
            return true;
        }

        Transform tr = entry.Target.transform;
        anchor = tr.position + Vector3.up * (1f + worldOffset);
        return true;
    }

    private bool TryGetBounds(HpBarEntry entry, out Bounds bounds)
    {
        bool found = false;
        bounds = default;

        Collider[] colliders = entry.Colliders;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                continue;

            if (!found)
            {
                bounds = col.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (found)
            return true;

        Renderer[] renderers = entry.Renderers;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                continue;

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }

    private bool TryEnsureOverlayRoot()
    {
        if (overlayRoot != null && overlayRoot.panel != null)
            return true;

        if (targetDocument == null)
            targetDocument = GetComponent<UIDocument>();

        if (targetDocument == null || targetDocument.rootVisualElement == null)
            targetDocument = FindFirstObjectByType<UIDocument>();

        if (targetDocument == null || targetDocument.rootVisualElement == null)
            return false;

        VisualElement root = targetDocument.rootVisualElement;
        overlayRoot = root.Q<VisualElement>(OverlayRootName);
        if (overlayRoot == null)
        {
            overlayRoot = new VisualElement();
            overlayRoot.name = OverlayRootName;
            overlayRoot.pickingMode = PickingMode.Ignore;
            overlayRoot.style.position = Position.Absolute;
            overlayRoot.style.left = 0f;
            overlayRoot.style.top = 0f;
            overlayRoot.style.right = 0f;
            overlayRoot.style.bottom = 0f;
            root.Add(overlayRoot);
        }

        return true;
    }

    private bool TryGetTemplateAsset(out VisualTreeAsset template)
    {
        template = hpBarTemplateAsset;
        if (template != null)
            return true;

        if (!templateWarningLogged)
        {
            templateWarningLogged = true;
            Debug.LogWarning("DamageHpOverlayUI: hpBarTemplateAsset is not assigned.");
        }

        return false;
    }

    private Camera ResolveCamera()
    {
        if (targetCamera != null)
            return targetCamera;

        targetCamera = Camera.main;
        return targetCamera;
    }

    private Color GetHpColor(float ratio)
    {
        if (ratio > 0.6f)
            return highHpColor;
        if (ratio > 0.3f)
            return midHpColor;
        return lowHpColor;
    }

    private void SetVisible(HpBarEntry entry, bool visible)
    {
        if (entry == null || entry.Root == null)
            return;

        entry.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static float GetResolvedWidth(VisualElement element, float fallback)
    {
        if (element == null)
            return fallback;

        float width = element.resolvedStyle.width;
        if (float.IsNaN(width) || width <= 0f)
            return fallback;

        return width;
    }

    private static float GetResolvedHeight(VisualElement element, float fallback)
    {
        if (element == null)
            return fallback;

        float height = element.resolvedStyle.height;
        if (float.IsNaN(height) || height <= 0f)
            return fallback;

        return height;
    }

    private void ClearAllBars()
    {
        foreach (var pair in entries)
        {
            HpBarEntry entry = pair.Value;
            if (entry != null && entry.Root != null)
                entry.Root.RemoveFromHierarchy();
        }

        entries.Clear();
        overlayRoot = null;
    }
    private sealed class HpBarEntry
    {
        public MonoBehaviour Target;
        public VisualElement Root;
        public VisualElement Fill;
        public Label HpLabel;
        public Label DamageLabel;
        public Collider[] Colliders;
        public Renderer[] Renderers;
        public int CurrentHp;
        public int MaxHp;
        public float VisibleUntil;

        public float DisplayRatio;
        public float FromRatio;
        public float ToRatio;
        public float RatioAnimStartTime;
        public float RatioAnimEndTime;

        public float DamageTextStartTime;
        public float DamageTextHoldEndTime;
        public float DamageTextEndTime;
        public float DamageTextBaseTop;
    }
}

