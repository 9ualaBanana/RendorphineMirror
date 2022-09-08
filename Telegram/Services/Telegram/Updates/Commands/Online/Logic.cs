namespace Telegram.Services.Telegram.Updates.Commands.Online;

internal static class Logic
{
    internal static string BuildMessage(int onlineNodes, int offlineNodes) =>
        $"Online: *{onlineNodes}*\nOffline: {offlineNodes}";
}
