using UnityEngine;
using UnityEngine.InputSystem;

// Common base for field input controllers across preparation and battle stages.
// It provides stage activation and shared field raycast/select helpers.
public abstract class FieldInputControllerBase : MonoBehaviour
{
    protected abstract StageType ActiveStage { get; }

    // Shared field-input gate across stage field controllers.
    protected static bool isInputEnabled = true;

    protected UnitBase selectedUnit;

    protected virtual void Awake()
    {
        EventBus.Instance.Subscribe<StageChangedEvent>(OnStageChanged);
        enabled = false;
    }

    protected virtual void OnDestroy()
    {
        EventBus.Instance.Unsubscribe<StageChangedEvent>(OnStageChanged);
    }

    private void OnStageChanged(StageChangedEvent e)
    {
        enabled = (e.CurrentStage == ActiveStage);

        if (!enabled)
            DeselectUnit();
    }

    protected static void SetFieldInputEnabled(bool enabledValue)
    {
        isInputEnabled = enabledValue;
    }

    protected static bool IsFieldInputEnabled()
    {
        return isInputEnabled;
    }

    protected bool TryGetHit(out RaycastHit hit)
    {
        if (Camera.main == null || Mouse.current == null)
        {
            hit = default;
            return false;
        }

        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out hit);
    }

    protected bool TryGetGroundHit(out RaycastHit groundHit)
    {
        if (Camera.main == null || Mouse.current == null)
        {
            groundHit = default;
            return false;
        }

        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return TryGetGroundHitFromRay(ray, out groundHit);
    }

    public static bool TryGetGroundHitFromRay(Ray ray, out RaycastHit groundHit)
    {
        var hits = Physics.RaycastAll(ray);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<UnitBase>() == null)
            {
                groundHit = h;
                return true;
            }
        }

        groundHit = default;
        return false;
    }

    protected void SelectUnit(UnitBase unit)
    {
        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanSelectUnit(unit))
            return;

        selectedUnit = unit;
        EventBus.Instance.Publish(new UnitSelectedEvent { Unit = unit });
    }

    protected void DeselectUnit()
    {
        if (selectedUnit == null) return;
        selectedUnit = null;
        EventBus.Instance.Publish(new UnitDeselectedEvent());
    }
}
