namespace Node.Common.Models;

public static class OperationResultExtensions
{
    static readonly ILogger Logger = new NLog.Extensions.Logging.NLogLoggerFactory().CreateLogger<OperationResult>();

    public static T LogIfError<T>(this T opres, string? format = null, LogLevel level = LogLevel.Error) where T : IOperationResult =>
        opres.LogIfError(Logger, format, level);
    public static Task<T> LogIfError<T>(this Task<T> opres, string? format = null, LogLevel level = LogLevel.Error) where T : IOperationResult =>
        opres.LogIfError(Logger, format, level);
}
