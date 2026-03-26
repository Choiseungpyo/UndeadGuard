using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 입력을 읽어 BattleController에 전달하는 입력 처리 클래스.
/// 유닛 클릭과 바닥 클릭을 구분하여 선택과 이동 요청으로 변환한다.
/// </summary>
public sealed class BattleInputController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask unitLayerMask;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private GridCoordinateMapper coordinateMapper;

    private BattleController controller;
    private GridPosition lastHoverPosition;
    private bool hasLastHoverPosition;

    public void Initialize(BattleController controller)
    {
        this.controller = controller;
    }

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        HandleHover(mouse.position.ReadValue());

        if (mouse.rightButton.wasPressedThisFrame)
        {
            controller.ClearSelection();
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick(mouse.position.ReadValue());
        }
    }

    private void HandleHover(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit groundHit, 1000f, groundLayerMask))
        {
            hasLastHoverPosition = false;
            return;
        }

        GridPosition hoverPosition = coordinateMapper.GetGridPosition(groundHit.point);

        if (hasLastHoverPosition && hoverPosition == lastHoverPosition)
        {
            return;
        }

        lastHoverPosition = hoverPosition;
        hasLastHoverPosition = true;

        controller.HandleGroundHover(hoverPosition);
    }

    private void HandleLeftClick(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit unitHit, 1000f, unitLayerMask))
        {
            UnitActor actor = unitHit.collider.GetComponent<UnitActor>();

            if (actor == null)
            {
                actor = unitHit.collider.GetComponentInParent<UnitActor>();
            }

            if (actor == null)
            {
                actor = unitHit.collider.GetComponentInChildren<UnitActor>();
            }

            if (actor != null)
            {
                controller.HandleUnitClick(actor.UnitId);
                return;
            }
        }

        if (!Physics.Raycast(ray, out RaycastHit groundHit, 1000f, groundLayerMask))
        {
            return;
        }

        GridPosition position = coordinateMapper.GetGridPosition(groundHit.point);
        controller.HandleGroundClick(position);
    }
}