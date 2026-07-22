namespace ArmoryManager;

internal static class ArmoryUi
{
    // Configuration intentionally lives only in Mod Manager. In-game UI is limited to brief feedback.
    internal static void Notify(string message, bool force = false)
    {
        Plugin.ModHelperInstance?.ShowNotification(message, true);
    }
}
