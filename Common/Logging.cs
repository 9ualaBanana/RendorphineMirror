using NLog.Targets;

namespace Common;

internal static class Logging
{
    readonly static string _layout = $"${{time:universalTime=true}} ${{pad:padding=-7:inner=[${{level:uppercase=true}}]}} ${{message:withException=true:exceptionSeparator=\n\n}}";

    readonly static string _logDir = "logs${dir-separator}${processname}${dir-separator}";
    readonly static FileTarget _file = new()
    {
        FileName = $"{_logDir}log{_fileExtension}",
        Layout = _layout,
        ArchiveEvery = FileArchivePeriod.Day,
        ArchiveDateFormat = "yyyyMMdd",
        ArchiveFileName = $"{_logDir}log.{{#}}{_fileExtension}",
        ArchiveNumbering = ArchiveNumberingMode.Date,
        MaxArchiveDays = 7
    };
    readonly static string _fileExtension = ".log";

    readonly static ColoredConsoleTarget _console = new()
    {
        DetectConsoleAvailable = true,
        Layout = _layout,
        AutoFlush = true,
        UseDefaultRowHighlightingRules = true
    };

    internal static void Configure(bool isDebug)
    {
        LogManager.AutoShutdown = true;
        LogManager.GlobalThreshold = LogLevel.Trace;
        LogManager.Setup().SetupLogFactory(config => config.SetTimeSourcAccurateUtc());
        LogManager.Setup().LoadConfiguration(rule => rule.ForLogger()
                .FilterMinLevel(LogLevel.Trace)
                .WriteTo(_file));
        LogManager.Setup().LoadConfiguration(rule => rule.ForLogger()
                .FilterMinLevel(isDebug ? LogLevel.Trace : LogLevel.Info)
                .WriteTo(_console));
    }
}
