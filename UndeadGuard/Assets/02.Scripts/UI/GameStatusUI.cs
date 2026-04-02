using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 왼쪽 상단 게임 진행 상태 UI
// GameProgressUpdatedEvent를 구독해 표시 데이터를 갱신한다
// 배치 단계와 전투 단계에서 서로 다른 레이아웃을 표시한다
//
// [배치 단계]  [전투 단계]
//  n일차        n일차
//  배치 단계    전투 단계
//  [배치 종료]  Phase X / Y
//               플레이어 턴 N / 적 턴
//               [턴 종료]
public class GameStatusUI : MonoBehaviour
{
    [Header("공통")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI stageText;

    [Header("전투 단계 전용")]
    [SerializeField] private GameObject phaseRow;
    [SerializeField] private TextMeshProUGUI phaseText;

    [SerializeField] private GameObject turnRow;
    [SerializeField] private TextMeshProUGUI turnText;

    [Header("버튼")]
    [SerializeField] private Button preparationEndButton;
    [SerializeField] private Button endTurnButton;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<GameProgressUpdatedEvent>(OnProgressUpdated);

        if (preparationEndButton != null)
            preparationEndButton.onClick.AddListener(OnPreparationEndClicked);

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<GameProgressUpdatedEvent>(OnProgressUpdated);

        if (preparationEndButton != null)
            preparationEndButton.onClick.RemoveListener(OnPreparationEndClicked);

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
    }

    // 진행 데이터를 받아 각 UI 요소를 갱신한다
    private void OnProgressUpdated(GameProgressUpdatedEvent e)
    {
        if (dayText != null)
            dayText.text = e.DayText;

        if (stageText != null)
            stageText.text = e.StageText;

        // Phase 줄: 전투 단계이며 Phase가 시작된 경우에만 표시
        if (phaseRow != null)
            phaseRow.SetActive(e.ShowPhaseText);

        if (phaseText != null)
            phaseText.text = e.PhaseText;

        // 턴 줄: 전투 단계이며 턴이 진행 중인 경우에만 표시
        if (turnRow != null)
            turnRow.SetActive(e.ShowTurnText);

        if (turnText != null)
            turnText.text = e.TurnText;

        // 버튼 표시: 배치 단계면 배치 종료, 전투 단계면 턴 종료
        if (preparationEndButton != null)
            preparationEndButton.gameObject.SetActive(e.IsPreparationStage);

        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(!e.IsPreparationStage);
    }

    // 배치 종료 버튼: 전투 단계 시작 요청
    private void OnPreparationEndClicked()
    {
        GamePhaseController.Instance.RequestNextWave();
    }

    // 턴 종료 버튼: EndTurnRequestedEvent 발행 (TurnManager가 처리)
    private void OnEndTurnClicked()
    {
        EventBus.Instance.Publish(new EndTurnRequestedEvent());
    }
}
