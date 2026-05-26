public static class UIFieldInputBlocker
{
    public static void RequestSkipNextWorldClick()
    {
        if (BattleInputGuard.TryGetExisting(out var guard))
            guard.RequestSkipNextWorldClick();
    }

    public static bool ConsumeSkipNextWorldClick()
    {
        return BattleInputGuard.TryGetExisting(out var guard) && guard.ConsumeSkipNextWorldClick();
    }

    public static void Clear()
    {
        if (BattleInputGuard.TryGetExisting(out var guard))
            guard.ClearSkipNextWorldClick();
    }
}
