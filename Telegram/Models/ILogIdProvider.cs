namespace Telegram.Models;

public interface ILogIdProvider
{
    string LogId { get; }
}

static class LogIdProviderExtensions
{
    public static void LogTraceUsing(this ILogIdProvider logIdProvider, ILogger logger, string? message, params object?[] args)
        => logger.LogTrace($"[{{LogId}}] {message}", args.Prepend(logIdProvider.LogId));
}
