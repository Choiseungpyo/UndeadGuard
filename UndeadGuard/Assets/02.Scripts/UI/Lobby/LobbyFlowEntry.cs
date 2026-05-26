using UnityEngine;

public sealed class LobbyFlowEntry : MonoBehaviour
{
    [SerializeField] private AppFlowSceneConfig sceneConfig;

    public void OnNewGameButtonPressed()
    {
        StartBattle();
    }

    public void OnContinueButtonPressed()
    {
        StartBattle();
    }

    public void OnStartBattleButtonPressed()
    {
        StartBattle();
    }

    public void OnSettingsButtonPressed()
    {
        string sceneName = sceneConfig != null ? sceneConfig.SettingsSceneName : string.Empty;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.Log("[LobbyFlowEntry] Settings is not implemented yet.");
            return;
        }

        EventBus.Instance.Publish(new RequestOpenSettingsEvent { SceneName = sceneName });
    }

    public void OnCreditsButtonPressed()
    {
        Debug.Log("[LobbyFlowEntry] Credits is not implemented yet.");
    }

    public void OnBackToTitleButtonPressed()
    {
        string sceneName = sceneConfig != null ? sceneConfig.TitleSceneName : string.Empty;
        EventBus.Instance.Publish(new RequestOpenTitleEvent { SceneName = sceneName });
    }

    private void StartBattle()
    {
        string sceneName = sceneConfig != null ? sceneConfig.BattleSceneName : string.Empty;
        EventBus.Instance.Publish(new RequestStartBattleEvent { SceneName = sceneName });
    }
}
