namespace Telegram.Models;

static class LogId
{
    public static string? Formatted(string? message, string logId, string? logIdName = null)
        => $"[{(logIdName is null ? string.Empty : logIdName+'|')}{logId}] {message}";
}
