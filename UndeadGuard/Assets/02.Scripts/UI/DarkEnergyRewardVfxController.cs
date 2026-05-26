using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DarkEnergyRewardVfxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument targetDocument;
    [SerializeField] private Camera targetCamera;

    [Header("UXML Names")]
    [SerializeField] private string darkEnergyTargetName = "DarkEnergyLabel";

    [Header("Pickup")]
    [SerializeField] private int maxPickupCount = 5;
    [SerializeField] private float pickupDuration = 1.45f;
    [SerializeField] private float pickupDelayStep = 0.12f;
    [SerializeField] private float pickupArcHeight = 95f;
    [SerializeField] private float pickupScatterRadius = 26f;
    [SerializeField] private float orbSize = 42f;
    [SerializeField] private float trailWidth = 80f;
    [SerializeField] private float trailHeight = 34f;
    [SerializeField] private float arrivalBurstSize = 92f;

    [Header("Death Residue")]
    [SerializeField] private float deathResidueDelay = 1.05f;
    [SerializeField] private float deathResidueAppearDuration = 0.28f;
    [SerializeField] private float deathResidueHoldDuration = 0.85f;
    [SerializeField] private float deathResidueFadeDuration = 0.22f;
    [SerializeField] private float deathResidueOrbSize = 54f;
    [SerializeField] private float deathResidueBurstSize = 96f;

    [Header("Wave Clear")]
    [SerializeField] private float waveRewardHoldDuration = 1.2f;
    [SerializeField] private float waveRewardFadeDuration = 0.25f;

    private VisualElement root;
    private VisualElement effectLayer;
    private VisualElement darkEnergyTarget;
    private Texture2D orbTexture;
    private Texture2D trailTexture;
    private Texture2D burstTexture;

    private void OnEnable()
    {
        ResolveReferences();
        LoadTextures();

        EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Subscribe<WaveClearedEvent>(OnWaveCleared);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Unsubscribe<WaveClearedEvent>(OnWaveCleared);
    }

    private void ResolveReferences()
    {
        if (targetDocument == null)
            targetDocument = GetComponent<UIDocument>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetDocument == null)
            return;

        root = targetDocument.rootVisualElement;
        darkEnergyTarget = root.Q<VisualElement>(darkEnergyTargetName);

        effectLayer = root.Q<VisualElement>("DarkEnergyEffectLayer");
        if (effectLayer == null)
        {
            effectLayer = new VisualElement { name = "DarkEnergyEffectLayer" };
            effectLayer.pickingMode = PickingMode.Ignore;
            effectLayer.style.position = Position.Absolute;
            effectLayer.style.left = 0;
            effectLayer.style.top = 0;
            effectLayer.style.right = 0;
            effectLayer.style.bottom = 0;
            root.Add(effectLayer);
        }
    }

    private void LoadTextures()
    {
        orbTexture = Resources.Load<Texture2D>("DarkEnergy/DarkEnergyOrb");
        trailTexture = Resources.Load<Texture2D>("DarkEnergy/DarkEnergyTrail");
        burstTexture = Resources.Load<Texture2D>("DarkEnergy/DarkEnergyBurst");
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        if (e.DarkEnergyReward <= 0)
        {
            EventBus.Instance.Publish(new EnemyKillRewardAbsorbedEvent { Unit = e.Unit });
            return;
        }

        StartCoroutine(PlayEnemyReward(e));
    }

    private IEnumerator PlayEnemyReward(EnemyDiedEvent e)
    {
        ResolveReferences();

        Vector2 start = WorldToPanelPosition(e.WorldPosition);
        Vector2 target = GetDarkEnergyTargetPosition();

        yield return PlayDeathResidue(start);

        int pickupCount = Mathf.Clamp(e.DarkEnergyReward, 1, maxPickupCount);
        int remaining = pickupCount;
        int grantedAmount = 0;

        for (int i = 0; i < pickupCount; i++)
        {
            int pickupAmount = SplitRewardAmount(e.DarkEnergyReward, pickupCount, i);
            grantedAmount += pickupAmount;
            StartCoroutine(PlayPickup(start, target, i, pickupAmount, () => remaining--));
            yield return new WaitForSeconds(pickupDelayStep);
        }

        while (remaining > 0)
            yield return null;

        if (grantedAmount > 0 && ResourceManager.Instance != null)
            ResourceManager.Instance.AddDarkEnergy(grantedAmount);

        EventBus.Instance.Publish(new EnemyKillRewardAbsorbedEvent { Unit = e.Unit });
    }

    private IEnumerator PlayDeathResidue(Vector2 position)
    {
        if (deathResidueDelay > 0f)
            yield return new WaitForSeconds(deathResidueDelay);

        VisualElement residueRoot = new VisualElement();
        residueRoot.pickingMode = PickingMode.Ignore;
        residueRoot.style.position = Position.Absolute;
        residueRoot.style.width = deathResidueBurstSize;
        residueRoot.style.height = deathResidueBurstSize;
        residueRoot.style.left = position.x - deathResidueBurstSize * 0.5f;
        residueRoot.style.top = position.y - deathResidueBurstSize * 0.5f;
        residueRoot.style.opacity = 0f;

        VisualElement burst = CreateImageElement(burstTexture, deathResidueBurstSize, deathResidueBurstSize);
        burst.style.position = Position.Absolute;
        burst.style.left = 0;
        burst.style.top = 0;
        burst.style.opacity = 0.55f;
        residueRoot.Add(burst);

        VisualElement orb = CreateImageElement(orbTexture, deathResidueOrbSize, deathResidueOrbSize);
        orb.style.position = Position.Absolute;
        orb.style.left = (deathResidueBurstSize - deathResidueOrbSize) * 0.5f;
        orb.style.top = (deathResidueBurstSize - deathResidueOrbSize) * 0.5f;
        residueRoot.Add(orb);

        effectLayer.Add(residueRoot);

        float elapsed = 0f;
        float appearDuration = Mathf.Max(0.01f, deathResidueAppearDuration);
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / appearDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            residueRoot.style.opacity = Mathf.Lerp(0f, 1f, eased);
            residueRoot.style.scale = new Scale(Vector3.one * Mathf.Lerp(0.55f, 1f, eased));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < deathResidueHoldDuration)
        {
            elapsed += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(elapsed * 8f) * 0.04f;
            orb.style.scale = new Scale(Vector3.one * pulse);
            burst.style.rotate = new Rotate(new Angle(elapsed * 22f, AngleUnit.Degree));
            yield return null;
        }

        elapsed = 0f;
        float fadeDuration = Mathf.Max(0.01f, deathResidueFadeDuration);
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            residueRoot.style.opacity = 1f - t;
            residueRoot.style.scale = new Scale(Vector3.one * Mathf.Lerp(1f, 0.8f, t));
            yield return null;
        }

        residueRoot.RemoveFromHierarchy();
    }

    private IEnumerator PlayPickup(Vector2 start, Vector2 target, int index, int amount, System.Action onComplete)
    {
        Vector2 scatter = Random.insideUnitCircle * pickupScatterRadius;
        Vector2 startWithScatter = start + scatter;
        Vector2 control = (startWithScatter + target) * 0.5f + Vector2.up * pickupArcHeight;

        VisualElement pickupRoot = new VisualElement();
        pickupRoot.pickingMode = PickingMode.Ignore;
        pickupRoot.style.position = Position.Absolute;
        pickupRoot.style.width = trailWidth;
        pickupRoot.style.height = Mathf.Max(orbSize, trailHeight);

        VisualElement trail = CreateImageElement(trailTexture, trailWidth, trailHeight);
        trail.style.position = Position.Absolute;
        trail.style.left = 0;
        trail.style.top = (Mathf.Max(orbSize, trailHeight) - trailHeight) * 0.5f;
        trail.style.opacity = 0.72f;
        pickupRoot.Add(trail);

        VisualElement orb = CreateImageElement(orbTexture, orbSize, orbSize);
        orb.style.position = Position.Absolute;
        orb.style.left = trailWidth - orbSize;
        orb.style.top = (Mathf.Max(orbSize, trailHeight) - orbSize) * 0.5f;
        pickupRoot.Add(orb);

        effectLayer.Add(pickupRoot);

        float elapsed = 0f;
        while (elapsed < pickupDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pickupDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector2 position = Bezier(startWithScatter, control, target, eased);
            Vector2 direction = target - position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            pickupRoot.style.left = position.x - trailWidth + orbSize * 0.5f;
            pickupRoot.style.top = position.y - Mathf.Max(orbSize, trailHeight) * 0.5f;
            pickupRoot.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
            pickupRoot.style.scale = new Scale(Vector3.one * Mathf.Lerp(1f, 0.55f, eased));
            pickupRoot.style.opacity = Mathf.Lerp(1f, 0.35f, Mathf.Max(0f, eased - 0.72f) / 0.28f);
            yield return null;
        }

        pickupRoot.RemoveFromHierarchy();
        yield return PlayArrivalBurst(target);
        onComplete?.Invoke();
    }

    private IEnumerator PlayArrivalBurst(Vector2 target)
    {
        VisualElement burst = CreateImageElement(burstTexture, arrivalBurstSize, arrivalBurstSize);
        burst.pickingMode = PickingMode.Ignore;
        burst.style.position = Position.Absolute;
        burst.style.left = target.x - arrivalBurstSize * 0.5f;
        burst.style.top = target.y - arrivalBurstSize * 0.5f;
        effectLayer.Add(burst);

        float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            burst.style.scale = new Scale(Vector3.one * Mathf.Lerp(0.5f, 1.25f, t));
            burst.style.opacity = 1f - t;
            yield return null;
        }

        burst.RemoveFromHierarchy();
    }

    private void OnWaveCleared(WaveClearedEvent e)
    {
        StartCoroutine(PlayWaveClearReward(e));
    }

    private IEnumerator PlayWaveClearReward(WaveClearedEvent e)
    {
        ResolveReferences();

        VisualElement panel = new VisualElement { name = "WaveClearRewardPanel" };
        panel.pickingMode = PickingMode.Ignore;
        panel.style.position = Position.Absolute;
        panel.style.left = Length.Percent(50);
        panel.style.top = Length.Percent(34);
        panel.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50), 0);
        panel.style.width = 420;
        panel.style.height = 168;
        panel.style.alignItems = Align.Center;
        panel.style.justifyContent = Justify.Center;
        panel.style.backgroundColor = new Color(0.04f, 0.02f, 0.08f, 0.86f);
        panel.style.borderTopWidth = 1;
        panel.style.borderBottomWidth = 1;
        panel.style.borderLeftWidth = 1;
        panel.style.borderRightWidth = 1;
        panel.style.borderTopColor = new Color(0.55f, 0.25f, 1f, 0.9f);
        panel.style.borderBottomColor = new Color(0.15f, 0.9f, 1f, 0.75f);
        panel.style.borderLeftColor = new Color(0.55f, 0.25f, 1f, 0.9f);
        panel.style.borderRightColor = new Color(0.15f, 0.9f, 1f, 0.75f);

        VisualElement burst = CreateImageElement(burstTexture, 210f, 210f);
        burst.style.position = Position.Absolute;
        burst.style.left = 105;
        burst.style.top = -24;
        burst.style.opacity = 0.55f;
        panel.Add(burst);

        Label title = new Label("WAVE CLEAR");
        title.style.fontSize = 34;
        title.style.color = new Color(0.85f, 0.95f, 1f, 1f);
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.marginBottom = 12;
        panel.Add(title);

        Label reward = new Label($"Dark Energy +{e.DarkEnergyReward} 획득");
        reward.style.fontSize = 22;
        reward.style.color = new Color(0.72f, 1f, 1f, 1f);
        reward.style.unityTextAlign = TextAnchor.MiddleCenter;
        panel.Add(reward);

        effectLayer.Add(panel);

        float elapsed = 0f;
        while (elapsed < waveRewardHoldDuration)
        {
            elapsed += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(elapsed * 7f) * 0.025f;
            burst.style.scale = new Scale(Vector3.one * pulse);
            yield return null;
        }

        if (e.DarkEnergyReward > 0 && ResourceManager.Instance != null)
            ResourceManager.Instance.AddWaveReward(e.DarkEnergyReward);

        elapsed = 0f;
        while (elapsed < waveRewardFadeDuration)
        {
            elapsed += Time.deltaTime;
            panel.style.opacity = 1f - Mathf.Clamp01(elapsed / waveRewardFadeDuration);
            yield return null;
        }

        panel.RemoveFromHierarchy();
        EventBus.Instance.Publish(new WaveClearRewardFinishedEvent { WaveNumber = e.WaveNumber });
    }

    private VisualElement CreateImageElement(Texture2D texture, float width, float height)
    {
        VisualElement element = new VisualElement();
        element.style.width = width;
        element.style.height = height;
        element.style.backgroundImage = texture != null ? new StyleBackground(texture) : default;
        return element;
    }

    private Vector2 WorldToPanelPosition(Vector3 worldPosition)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (root == null || root.panel == null || targetCamera == null)
            return Vector2.zero;

        Vector2 screenPosition = targetCamera.WorldToScreenPoint(worldPosition);
        screenPosition.y = Screen.height - screenPosition.y;
        return RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
    }

    private Vector2 GetDarkEnergyTargetPosition()
    {
        if (darkEnergyTarget == null)
            return new Vector2(160f, 72f);

        Rect bounds = darkEnergyTarget.worldBound;
        return new Vector2(bounds.center.x, bounds.center.y);
    }

    private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * a
            + 2f * oneMinusT * t * b
            + t * t * c;
    }

    private static int SplitRewardAmount(int totalAmount, int pickupCount, int index)
    {
        int baseAmount = totalAmount / pickupCount;
        int remainder = totalAmount % pickupCount;
        return baseAmount + (index < remainder ? 1 : 0);
    }
}
