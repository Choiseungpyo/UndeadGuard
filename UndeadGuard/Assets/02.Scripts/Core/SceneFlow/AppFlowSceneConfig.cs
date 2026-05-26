using UnityEngine;

[CreateAssetMenu(fileName = "AppFlowSceneConfig", menuName = "UndeadGuard/App Flow Scene Config")]
public sealed class AppFlowSceneConfig : ScriptableObject
{
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private string introCutsceneSceneName = "IntroCutscene";
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string battleSceneName = "Main";
    [SerializeField] private string resultSceneName = "Result";
    [SerializeField] private string settingsSceneName = "";

    public string TitleSceneName => titleSceneName;
    public string IntroCutsceneSceneName => introCutsceneSceneName;
    public string LobbySceneName => lobbySceneName;
    public string BattleSceneName => battleSceneName;
    public string ResultSceneName => resultSceneName;
    public string SettingsSceneName => settingsSceneName;

    public string ResolveSceneName(AppFlowState state)
    {
        switch (state)
        {
            case AppFlowState.Title:
                return titleSceneName;
            case AppFlowState.Cutscene:
                return introCutsceneSceneName;
            case AppFlowState.Lobby:
                return lobbySceneName;
            case AppFlowState.Battle:
                return battleSceneName;
            case AppFlowState.Result:
                return resultSceneName;
            case AppFlowState.Settings:
                return settingsSceneName;
            default:
                return string.Empty;
        }
    }
}
