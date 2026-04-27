public static class UIWorldInputGuard
{
    private static bool skipNextWorldClick;

    public static void RequestSkipNextWorldClick()
    {
        skipNextWorldClick = true;
    }

    public static bool ConsumeSkipNextWorldClick()
    {
        if (!skipNextWorldClick)
            return false;

        skipNextWorldClick = false;
        return true;
    }

    public static void Clear()
    {
        skipNextWorldClick = false;
    }
}