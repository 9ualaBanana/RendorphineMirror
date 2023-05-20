using System.Management.Automation;

namespace Node.Registry;

[Cmdlet("Write", "Log")]
public class WriteLogCmdlet : PSCmdlet
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Parameter(Position = 0, Mandatory = true)]
    public string Level { get; set; } = null!;

    [Parameter(Position = 1, Mandatory = true)]
    public string Text { get; set; } = null!;

    protected override void ProcessRecord()
    {
        base.ProcessRecord();

        var level = Level.ToLowerInvariant() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "warn" => LogLevel.Warn,
            "error" => LogLevel.Error,

            _ => throw new ArgumentException("Unknown log level"),
        };

        Logger.Log(level, Text);
    }
}
