﻿using NLog.Targets;

namespace Common;

internal static class Logging
{
    readonly static string _layout = $"${{time:universalTime=true}} ${{pad:padding=-7:inner=[${{level:uppercase=true}}]}} ${{message:withException=true:exceptionSeparator=\n\n}}";


    internal static void Configure(bool isDebug)
    {
        LogManager.AutoShutdown = true;
        LogManager.GlobalThreshold = LogLevel.Trace;
        LogManager.Setup().SetupLogFactory(config => config.SetTimeSourcAccurateUtc());
        SetupRuleFor(LogLevel.Debug);
        SetupRuleFor(LogLevel.Trace);
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

    static void SetupRuleFor(LogLevel logLevel) => LogManager.Setup()
        .LoadConfiguration(rule => rule.ForLogger()
        .FilterMinLevel(logLevel)
        .WriteTo(FileTargetWith(logLevel)));

    static FileTarget FileTargetWith(LogLevel logLevel) => new()
    {
        FileName = $"{_LogDirFor(logLevel)}log{_fileExtension}",
        Layout = _layout,
        ArchiveEvery = FileArchivePeriod.Day,
        ArchiveDateFormat = "yyyyMMdd",
        ArchiveFileName = $"{_LogDirFor}log.{{#}}{_fileExtension}",
        ArchiveNumbering = ArchiveNumberingMode.Date,
        MaxArchiveDays = 7
    };

    static string _LogDirFor(LogLevel logLevel) => $"{_logDir}{logLevel.Name}";
    readonly static string _logDir = "logs${dir-separator}${processname}${dir-separator}";
    readonly static string _fileExtension = ".log";
}
