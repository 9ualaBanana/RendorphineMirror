using System.Management.Automation;

namespace Node.Registry;

[Cmdlet("Init", "Conda-Env")]
public class InitCondaEnvCmdlet : PSCmdlet
{
    [Parameter(Position = 0, Mandatory = true)]
    public string PythonVersion { get; set; } = null!;

    [Parameter(Position = 1, Mandatory = true)]
    public string[] Requirements { get; set; } = null!;

    [Parameter(Position = 2, Mandatory = true)]
    public string[] Channels { get; set; } = null!;

    [Parameter(Mandatory = false)]
    public string[]? PipRequirements { get; set; }

    protected override void ProcessRecord()
    {
        base.ProcessRecord();

        var name = $"{GetVariableValue("PLUGIN").ToString()!}_{GetVariableValue("PLUGINVER").ToString()!}";

        var log = $"Initializing conda environment {name} with python={PythonVersion}";
        log += $"; requirements {string.Join(' ', Requirements)}";
        log += $"; channels {string.Join(' ', Channels)}";
        if (PipRequirements is not null)
            log += $"; pip {string.Join(' ', PipRequirements)}";

        LogManager.GetCurrentClassLogger().Info(log);
        CondaManager.InitializeEnvironment(PluginType.Conda.GetInstance().Path, name, PythonVersion, Requirements, Channels, PipRequirements);
    }
}
