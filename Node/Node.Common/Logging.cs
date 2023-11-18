using NLog;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace Node.Common;

public static class Logging
{
    public record Config()
    {
        public bool LogToFile { get; init; } = true;
        public int MaxLogsDebug { get; init; } = 3;
        public int MaxLogsTrace { get; init; } = 1;
    };

    const string Layout = "${time:universalTime=true} [${level:uppercase=true} @ ${logger:shortName=true} @ ${scopenested:separator= @ }] ${message:withException=true:exceptionSeparator=\n\n}";


    public static void Configure(bool isDebug, Config config)
    {
        LogManager.AutoShutdown = true;
        LogManager.GlobalThreshold = LogLevel.Trace;

        LogManager.Setup()
            .SetupLogFactory(config => config.SetTimeSourcAccurateUtc())
            .LoadConfiguration(rule => rule.ForLogger()
                .FilterMinLevel(isDebug ? LogLevel.Trace : LogLevel.Info)
                .WriteTo(CreateConsoleTarget())
            );

        if (config.LogToFile)
        {
            LogManager.Setup()
                .SetupFileRuleWith(LogLevel.Debug, maxArchiveDays: config.MaxLogsDebug)
                .SetupFileRuleWith(LogLevel.Trace, maxArchiveDays: config.MaxLogsTrace);
        }
    }

    static NLog.Config.ISetupBuilder SetupFileRuleWith(this NLog.Config.ISetupBuilder builder, LogLevel logLevel, int maxArchiveDays) =>
        builder
            .LoadConfiguration(rule => rule.ForLogger()
            .FilterMinLevel(logLevel)
            .WriteTo(CreateFileTarget(logLevel, maxArchiveDays)));


    static ColoredConsoleTarget CreateConsoleTarget() => new()
    {
        Layout = Layout,
        AutoFlush = true,
        DetectConsoleAvailable = true,
        UseDefaultRowHighlightingRules = true,
    };
    static FileTarget CreateFileTarget(LogLevel logLevel, int maxArchiveDays)
    {
        var extension = ".log";
        var dir = "logs${dir-separator}${processname}${dir-separator}" + logLevel.Name + "${dir-separator}";

        return new()
        {
            Layout = Layout,
            FileName = $"{dir}log{extension}",
            ArchiveEvery = FileArchivePeriod.Day,
            ArchiveDateFormat = "yyyyMMdd",
            ArchiveFileName = $"{dir}log.{{#}}{extension}",
            ArchiveNumbering = ArchiveNumberingMode.Date,
            MaxArchiveDays = maxArchiveDays,
        };
    }
}
