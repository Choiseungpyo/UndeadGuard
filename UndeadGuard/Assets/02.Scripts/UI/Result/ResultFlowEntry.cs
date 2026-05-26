using UnityEngine;

public sealed class ResultFlowEntry : MonoBehaviour
{
    public void OnContinueToLobbyButtonPressed()
    {
        EventBus.Instance.Publish(new RequestOpenLobbyEvent());
    }

    public void OnRetryBattleButtonPressed()
    {
        EventBus.Instance.Publish(new RequestRestartBattleEvent());
    }
}
