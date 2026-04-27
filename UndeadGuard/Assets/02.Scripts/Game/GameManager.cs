using UnityEngine;

// 씬의 게임 진행 시스템을 초기화하고 정리하는 진입점
// 하이라키 오브젝트가 필요 없는 시스템(GameStageController, GameProgress)을
// 내부에서 직접 생성하여 하이라키 오브젝트 수를 줄인다
//
// 실행 순서
//   Awake: 순수 C# 시스템 생성 (EventBus 구독 등록)
//   Start: GameStageController.Begin 호출 (모든 MonoBehaviour Awake 완료 후)
//   OnDestroy: 구독 해제
public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        new GameProgress();
        new GameStageController();
    }

    private void Start()
    {
        GameStageController.Instance.Begin();
    }

    private void OnDestroy()
    {
        GameStageController.Instance?.Cleanup();
        GameProgress.Instance?.Cleanup();
    }
}
