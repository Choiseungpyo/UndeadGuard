using UnityEngine;
using UnityEngine.InputSystem;

public class TacticalCameraController : MonoBehaviour
{
    [Header("Pan")]
    [SerializeField] private float keyboardPanSpeed = 12f;
    [SerializeField] private float dragPanSpeed = 0.025f;

    [Header("Zoom")]
    [SerializeField] private float zoomStep = 0.03f;
    [SerializeField] private float minHeight = 8f;
    [SerializeField] private float maxHeight = 45f;

    [Header("Focus")]
    [SerializeField] private Transform coreFocusTarget;
    [SerializeField] private Vector3 selectedFocusOffset = Vector3.zero;
    [SerializeField] private Vector3 coreFocusOffset = Vector3.zero;

    private BattleInputGuard inputGuard;
    private Transform selectedTarget;
    private bool isMouseCameraInputActive;

    private void Awake()
    {
        inputGuard = BattleInputGuard.Instance;

        if (coreFocusTarget == null && CoreHealth.Instance != null)
            coreFocusTarget = CoreHealth.Instance.transform;
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);

        SetMouseCameraInputState(false);
    }

    private void Update()
    {
        if (inputGuard == null)
            return;

        HandleMouseCameraInput();
        HandleKeyboardCameraInput();
    }

    public void SetSelectedTarget(Transform target)
    {
        selectedTarget = target;
    }

    private bool IsSelectionPriorityInputActive()
    {
        return selectedTarget != null;
    }

    private void HandleMouseCameraInput()
    {
        if (Mouse.current == null)
        {
            SetMouseCameraInputState(false);
            return;
        }

        if (IsSelectionPriorityInputActive())
        {
            SetMouseCameraInputState(false);
            return;
        }

        bool canUseMouseCameraInput = inputGuard.CanProcessCameraMouseInput();

        bool isPanDragPressed = Mouse.current.middleButton.isPressed;
        bool shouldCaptureMouseCameraInput = canUseMouseCameraInput && isPanDragPressed;
        SetMouseCameraInputState(shouldCaptureMouseCameraInput);

        if (!canUseMouseCameraInput)
            return;

        HandleZoom();

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        if (isPanDragPressed)
            HandleMiddleDragPan(mouseDelta);
    }

    private void HandleKeyboardCameraInput()
    {
        if (Keyboard.current == null)
            return;

        if (!inputGuard.CanProcessCameraKeyboardInput())
            return;

        Vector2 axis = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) axis.y += 1f;
        if (Keyboard.current.sKey.isPressed) axis.y -= 1f;
        if (Keyboard.current.dKey.isPressed) axis.x += 1f;
        if (Keyboard.current.aKey.isPressed) axis.x -= 1f;

        if (axis.sqrMagnitude > 0f)
            PanByKeyboard(axis.normalized);

        if (Keyboard.current.fKey.wasPressedThisFrame)
            FocusSelectedTarget();

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            FocusCoreTarget();
    }

    private void HandleMiddleDragPan(Vector2 mouseDelta)
    {
        var right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        var worldDelta = (-right * mouseDelta.x) + (-forward * mouseDelta.y);
        transform.position += worldDelta * dragPanSpeed;
    }

    private void HandleZoom()
    {
        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scrollY, 0f))
            return;

        var next = transform.position + (transform.forward * (scrollY * zoomStep));
        next.y = Mathf.Clamp(next.y, minHeight, maxHeight);
        transform.position = next;
    }

    private void PanByKeyboard(Vector2 axis)
    {
        var right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        var move = (right * axis.x + forward * axis.y) * keyboardPanSpeed * Time.deltaTime;
        transform.position += move;
    }

    private void FocusSelectedTarget()
    {
        if (selectedTarget == null)
            return;

        FocusWorldPoint(selectedTarget.position + selectedFocusOffset);
    }

    private void FocusCoreTarget()
    {
        if (coreFocusTarget == null)
        {
            CoreHealth core = CoreHealth.Instance;
            if (core != null)
                coreFocusTarget = core.transform;
        }

        if (coreFocusTarget == null)
            return;

        FocusWorldPoint(coreFocusTarget.position + coreFocusOffset);
    }

    private void FocusWorldPoint(Vector3 point)
    {
        var next = transform.position;
        next.x = point.x;
        next.z = point.z;
        transform.position = next;
    }

    private void SetMouseCameraInputState(bool value)
    {
        if (isMouseCameraInputActive == value)
            return;

        isMouseCameraInputActive = value;
        if (inputGuard != null)
            inputGuard.SetCameraMouseInputActive(value);
    }

    private void OnUnitSelected(UnitSelectedEvent e)
    {
        SetSelectedTarget(e.Unit != null ? e.Unit.transform : null);
    }

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        SetSelectedTarget(null);
    }
}
