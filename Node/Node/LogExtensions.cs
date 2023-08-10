using Microsoft.Extensions.Logging;

namespace Node;

public static class LogExtensions
{
    public static void Info(this ILogger logger, string text) => logger.LogInformation(text);
    public static void Trace(this ILogger logger, string text) => logger.LogTrace(text);
    public static void Warn(this ILogger logger, string text) => logger.LogWarning(text);

    public static void Error(this ILogger logger, string text) => logger.LogError(text);
    public static void Error(this ILogger logger, Exception ex, string? message = null) => logger.LogError(ex, message);
}
