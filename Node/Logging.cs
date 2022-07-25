global using NLog;
using NLog.Targets;

namespace Node;

internal static class Logging
{
    const string _layout = "${time:universalTime=true} [${uppercase:${level}}] ${logger}${newline}${message:withException=true:exceptionSeparator=\n\n}";
    readonly static string _logDir = $"logs{Path.DirectorySeparatorChar}";
    readonly static FileTarget _file = new()
    {
        FileName = $"{_logDir}log",
        Layout = _layout,
        ArchiveEvery = FileArchivePeriod.Day,
        ArchiveDateFormat = "yyyyMMdd",
        ArchiveFileName = $"{_logDir}log.{{#}}",
        ArchiveNumbering = ArchiveNumberingMode.Date,
        MaxArchiveDays = 30
    };

    internal static void Configure()
    {
        LogManager.AutoShutdown = true;
        LogManager.GlobalThreshold = LogLevel.Debug;
        LogManager.Setup().SetupLogFactory(config => config.SetTimeSourcAccurateUtc());
        LogManager.Setup().LoadConfiguration(rule => rule.ForLogger().WriteTo(_file));
    }
}
