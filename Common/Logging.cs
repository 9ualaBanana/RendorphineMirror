using NLog.Targets;

namespace Common;

internal static class Logging
{
    readonly static string _layout = "${time:universalTime=true} [${level:uppercase=true} @ ${logger:shortName=true} @ ${scopenested:separator= @ }] ${message:withException=true:exceptionSeparator=\n\n}";


    internal static void Configure(bool isDebug)
    {
        LogManager.AutoShutdown = true;
        LogManager.GlobalThreshold = LogLevel.Trace;
        LogManager.Setup().SetupLogFactory(config => config.SetTimeSourcAccurateUtc());

        if (Initializer.LogToFile)
        {
            SetupFileRuleWith(LogLevel.Debug, maxArchiveDays: 3);
            SetupFileRuleWith(LogLevel.Trace, maxArchiveDays: 1);
        }

        LogManager.Setup().LoadConfiguration(rule => rule.ForLogger()
                .FilterMinLevel(isDebug ? LogLevel.Trace : LogLevel.Info)
                .WriteTo(_console));
    }


    readonly static ColoredConsoleTarget _console = new()
    {
        DetectConsoleAvailable = true,
        Layout = _layout,
        AutoFlush = true,
        UseDefaultRowHighlightingRules = true
    };

    static void SetupFileRuleWith(LogLevel logLevel, int maxArchiveDays) => LogManager.Setup()
        .LoadConfiguration(rule => rule.ForLogger()
        .FilterMinLevel(logLevel)
        .WriteTo(FileTargetWith(logLevel, maxArchiveDays)));

    static FileTarget FileTargetWith(LogLevel logLevel, int maxArchiveDays) => new()
    {
        FileName = $"{_LogDirFor(logLevel)}log{_fileExtension}",
        Layout = _layout,
        ArchiveEvery = FileArchivePeriod.Day,
        ArchiveDateFormat = "yyyyMMdd",
        ArchiveFileName = $"{_LogDirFor(logLevel)}log.{{#}}{_fileExtension}",
        ArchiveNumbering = ArchiveNumberingMode.Date,
        MaxArchiveDays = maxArchiveDays
    };

    static string _LogDirFor(LogLevel logLevel) => $"{_logDir}{logLevel.Name}${{dir-separator}}";
    readonly static string _logDir = "logs${dir-separator}${processname}${dir-separator}";
    readonly static string _fileExtension = ".log";
}
