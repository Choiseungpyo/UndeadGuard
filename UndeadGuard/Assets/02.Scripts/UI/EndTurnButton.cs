using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 턴 종료 버튼 UI.
/// BattleTurnFlow에 플레이어 턴 종료를 요청한다.
/// </summary>
public sealed class EndTurnButton : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private BattleTurnFlow battleTurnFlow;

    private Button endTurnButton;

    private void Awake()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("UIDocument가 연결되지 않았음");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        endTurnButton = root.Q<Button>("End_Turn_Button");

        if (endTurnButton == null)
        {
            Debug.LogWarning("End_Turn_Button을 찾지 못함");
            return;
        }

        endTurnButton.clicked += HandleClick;
    }

    private void OnDestroy()
    {
        if (endTurnButton != null)
        {
            endTurnButton.clicked -= HandleClick;
        }
    }

    private void HandleClick()
    {
        if (battleTurnFlow == null)
        {
            Debug.LogWarning("BattleTurnFlow가 연결되지 않았음");
            return;
        }

        battleTurnFlow.RequestEndPlayerTurn();
    }
}