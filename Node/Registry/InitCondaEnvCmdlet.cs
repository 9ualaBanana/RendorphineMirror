using System.Management.Automation;
using Node.Plugins;

namespace Node.Registry;

[Cmdlet("Init", "Conda-Env")]
public class InitCondaEnvCmdlet : PSCmdlet
{
    [Parameter(Position = 0, Mandatory = true)]
    public string Name { get; set; } = null!;

    [Parameter(Position = 1, Mandatory = true)]
    public string PythonVersion { get; set; } = null!;

    [Parameter(Position = 2, Mandatory = true)]
    public string[] Requirements { get; set; } = null!;

    [Parameter(Mandatory = false)]
    public string[] Channels { get; set; } = null!;

    protected override void ProcessRecord()
    {
        base.ProcessRecord();

        LogManager.GetCurrentClassLogger().Info(
            $"Initializing conda environment {Name} with python={PythonVersion}; requirements {string.Join(' ', Requirements)}; channels {string.Join(' ', Channels)}");

        CondaManager.Initialize(Name, PythonVersion, Requirements, Channels);
    }
}
