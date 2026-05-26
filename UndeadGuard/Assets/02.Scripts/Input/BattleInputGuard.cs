using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Central guard that decides whether each input channel can run.
// It does not perform raycasts, selection, tile actions, or camera movement.
//
// Lifecycle note:
// BattleInputGuard is scene-authored. Place one instance in battle scenes that need
// field/camera/UI input gating.
public sealed class BattleInputGuard : MonoBehaviour
{
    // Singleton lifecycle.
    private static BattleInputGuard instance;
    private static bool hasWarnedMissingInstance;

    [SerializeField] private bool autoCollectUidocuments = true;

    // UI/input state tracking.
    private readonly List<UIDocument> uiDocuments = new List<UIDocument>();
    private readonly HashSet<object> systemLockOwners = new HashSet<object>();

    private bool skipNextWorldClick;
    private bool isCameraMouseInputActive;
    private bool isActionCameraActive;

    public static BattleInputGuard Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindFirstObjectByType<BattleInputGuard>();
            if (instance != null)
                return instance;

            if (!hasWarnedMissingInstance)
            {
                Debug.LogWarning("[BattleInputGuard] No scene instance found. Add BattleInputGuard to the active battle scene.");
                hasWarnedMissingInstance = true;
            }

            return null;
        }
    }

    // Teardown-safe accessor: never searches or creates a new instance.
    // Use this from OnDisable/OnDestroy-like paths.
    public static bool TryGetExisting(out BattleInputGuard guard)
    {
        guard = instance;
        return guard != null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Reset static state on domain reload / play-mode bootstrap.
        instance = null;
        hasWarnedMissingInstance = false;
    }

    public bool IsCameraMouseInputActive => isCameraMouseInputActive;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        hasWarnedMissingInstance = false;
        ResetTransientInputState();

        if (autoCollectUidocuments)
            RefreshUIDocuments();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void RegisterDocument(UIDocument document)
    {
        if (document == null)
            return;

        if (!uiDocuments.Contains(document))
            uiDocuments.Add(document);
    }

    public void UnregisterDocument(UIDocument document)
    {
        if (document == null)
            return;

        uiDocuments.Remove(document);
    }

    public void RefreshUIDocuments()
    {
        for (int i = uiDocuments.Count - 1; i >= 0; i--)
        {
            UIDocument document = uiDocuments[i];
            if (document == null || !document.isActiveAndEnabled)
                uiDocuments.RemoveAt(i);
        }

        if (!autoCollectUidocuments)
            return;

        var documents = FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < documents.Length; i++)
            RegisterDocument(documents[i]);
    }

    public void SetSystemLock(object owner, bool locked)
    {
        if (owner == null)
            return;

        if (locked)
            systemLockOwners.Add(owner);
        else
            systemLockOwners.Remove(owner);
    }

    public void SetCameraMouseInputActive(bool active)
    {
        isCameraMouseInputActive = active;
    }

    public void SetActionCameraActive(bool active)
    {
        isActionCameraActive = active;
    }

    public bool IsActionCameraActive()
    {
        return isActionCameraActive;
    }

    public void SetCameraDragging(bool dragging)
    {
        SetCameraMouseInputActive(dragging);
    }

    public void RequestSkipNextWorldClick()
    {
        skipNextWorldClick = true;
    }

    public void ClearSkipNextWorldClick()
    {
        skipNextWorldClick = false;
    }

    public bool ConsumeSkipNextWorldClick()
    {
        if (!skipNextWorldClick)
            return false;

        skipNextWorldClick = false;
        return true;
    }

    public bool CanProcessCameraMouseInput()
    {
        if (IsPointerBlockedByUI())
            return false;

        return !HasGlobalInputLock();
    }

    public bool CanProcessCameraKeyboardInput()
    {
        if (IsKeyboardBlockedByTextInput())
            return false;

        return !HasGlobalInputLock();
    }

    public bool CanProcessWorldClickInput()
    {
        if (IsPointerBlockedByUI())
            return false;

        if (HasGlobalInputLock())
            return false;

        return !isCameraMouseInputActive;
    }

    public bool CanProcessUnitCommandInput()
    {
        return CanProcessWorldClickInput();
    }

    public bool TryConsumeWorldClickPermission()
    {
        if (!CanProcessWorldClickInput())
            return false;

        if (ConsumeSkipNextWorldClick())
            return false;

        return true;
    }

    public bool IsPointerBlockedByUI()
    {
        if (Mouse.current == null)
            return false;

        var mousePosition = Mouse.current.position.ReadValue();
        bool hasActiveUiToolkitDocument = false;

        for (int i = uiDocuments.Count - 1; i >= 0; i--)
        {
            if (!TryGetActiveRoot(uiDocuments[i], out var root))
            {
                uiDocuments.RemoveAt(i);
                continue;
            }

            hasActiveUiToolkitDocument = true;

            var panel = root.panel;
            var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, mousePosition);
            var picked = panel.Pick(panelPosition);

            if (picked == null)
                continue;

            if (IsBlockingElementInHierarchy(picked, root))
                return true;
        }

        // Fall back to EventSystem only when no UI Toolkit document is active.
        if (!hasActiveUiToolkitDocument && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        return false;
    }

    public bool IsKeyboardBlockedByTextInput()
    {
        for (int i = uiDocuments.Count - 1; i >= 0; i--)
        {
            if (!TryGetActiveRoot(uiDocuments[i], out var root))
            {
                uiDocuments.RemoveAt(i);
                continue;
            }

            var focusedElement = root.panel.focusController != null
                ? root.panel.focusController.focusedElement as VisualElement
                : null;

            if (IsTextInputElement(focusedElement))
                return true;
        }

        return false;
    }

    private bool IsSystemLocked()
    {
        systemLockOwners.RemoveWhere(IsNullOrDestroyedOwner);
        return systemLockOwners.Count > 0;
    }

    private void ResetTransientInputState()
    {
        skipNextWorldClick = false;
        isCameraMouseInputActive = false;
        isActionCameraActive = false;
        systemLockOwners.Clear();
    }

    private bool HasGlobalInputLock()
    {
        return IsSystemLocked() || isActionCameraActive;
    }

    private static bool TryGetActiveRoot(UIDocument document, out VisualElement root)
    {
        root = null;
        if (document == null || !document.isActiveAndEnabled)
            return false;

        root = document.rootVisualElement;
        if (root == null || root.panel == null)
            return false;

        return true;
    }

    private static bool IsBlockingElementInHierarchy(VisualElement picked, VisualElement root)
    {
        for (var cursor = picked; cursor != null; cursor = cursor.parent)
        {
            if (IsBlockingElement(cursor))
                return true;

            if (cursor == root)
                break;
        }

        return false;
    }

    private static bool IsBlockingElement(VisualElement element)
    {
        if (element == null)
            return false;

        if (element.pickingMode == PickingMode.Ignore)
            return false;

        if (element is Button)
            return true;

        if (IsTextInputElement(element))
            return true;

        if (element.focusable)
            return true;

        return HasVisibleSurface(element);
    }

    private static bool IsNullOrDestroyedOwner(object owner)
    {
        if (owner == null)
            return true;

        if (owner is Object unityObject)
            return unityObject == null;

        return false;
    }

    private static bool HasVisibleSurface(VisualElement element)
    {
        var style = element.resolvedStyle;
        if (style.display == DisplayStyle.None)
            return false;

        if (style.visibility == Visibility.Hidden)
            return false;

        if (style.opacity <= 0f)
            return false;

        if (style.backgroundColor.a > 0.001f)
            return true;

        if (style.borderTopWidth > 0f && style.borderTopColor.a > 0.001f)
            return true;

        if (style.borderRightWidth > 0f && style.borderRightColor.a > 0.001f)
            return true;

        if (style.borderBottomWidth > 0f && style.borderBottomColor.a > 0.001f)
            return true;

        if (style.borderLeftWidth > 0f && style.borderLeftColor.a > 0.001f)
            return true;

        return false;
    }

    private static bool IsTextInputElement(VisualElement element)
    {
        for (var cursor = element; cursor != null; cursor = cursor.parent)
        {
            if (cursor is TextField)
                return true;

            if (cursor.ClassListContains("unity-base-text-field") || cursor.ClassListContains("unity-text-field"))
                return true;

            var typeName = cursor.GetType().Name;
            if (typeName.Contains("TextInput"))
                return true;
        }

        return false;
    }
}
