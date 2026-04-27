using UnityEngine;
using UnityEngine.InputSystem;

// 단계별 입력 컨트롤러의 공통 기반 클래스
// Awake/OnDestroy에서 구독하여 enabled 상태와 무관하게 StageChangedEvent를 수신한다
// enabled = false여도 이벤트를 받아 스스로 재활성화할 수 있다
public abstract class BaseInputController : MonoBehaviour
{
    protected abstract StageType ActiveStage { get; }

    // 두 컨트롤러 중 하나만 활성화되므로 static으로 공유한다
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

    protected bool TryGetHit(out RaycastHit hit)
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out hit);
    }

    protected bool TryGetGroundHit(out RaycastHit groundHit)
    {
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
