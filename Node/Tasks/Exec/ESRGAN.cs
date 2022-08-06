namespace Node.Tasks.Exec;

public class UpscaleEsrganInfo
{

}
public class EsrganTasks : ProcessTaskExecutor<UpscaleEsrganInfo>
{
    public static readonly EsrganTasks Instance = new();

    public readonly PluginAction<UpscaleEsrganInfo> UpscaleEsrgan;

    private EsrganTasks()
    {
        UpscaleEsrgan = new(PluginType.FFmpeg, nameof(UpscaleEsrgan), FileFormat.Jpeg, Start);
    }

    public override IEnumerable<IPluginAction> GetTasks() => new IPluginAction[] { UpscaleEsrgan };

    protected override string GetArguments(TaskExecuteData task, UpscaleEsrganInfo data)
    {
        var plugindir = Path.GetDirectoryName(task.Plugin.Path);

        var installfile = Path.GetTempFileName();

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var script = $@"
                Set-Location ""{plugindir}""
                {PluginType.Python.GetInstance().Path} -m venv venv
                ./venv/Scripts/activate
                pip3 install -r ./installation/requirements.txt
                foreach($line in Get-Content ./installation/precommands.txt){{
                    Invoke-Expression $line
                }}

                python test.py ""{task.Input}"" ""{task.Output}""
            ";
            File.WriteAllText(installfile, script);
        }
        else
        {
            var script = $@"
                #!/bin/bash

                cd ""{plugindir}""
                {PluginType.Python.GetInstance().Path} -m venv venv
                source ./venv/bin/activate
                pip3 install -r ./installation/requirements.txt
                sh ./installation/precommands.txt

                python test.py ""{task.Input}"" ""{task.Output}""
            ";
            File.WriteAllText(installfile, script);
        }

        return installfile;
    }

    protected override string GetExecutable(TaskExecuteData task) => Environment.OSVersion.Platform == PlatformID.Win32NT ? "powershell.exe" : "/bin/sh";
}