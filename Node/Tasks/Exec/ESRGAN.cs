namespace Node.Tasks.Exec;

public class UpscaleEsrganInfo { }
public static class EsrganTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new UpscaleEsrgan() };


    class UpscaleEsrgan : PluginAction<UpscaleEsrganInfo>
    {
        public override string Name => "UpscaleEsrgan";
        public override PluginType Type => PluginType.Python_Esrgan;
        public override FileFormat FileFormat => FileFormat.Jpeg;

        protected override async Task<string> Execute(ReceivedTask task, UpscaleEsrganInfo data)
        {
            var output = GetTaskOutputFile(task);
            var exepath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "powershell.exe" : "/bin/sh";
            var args = getScriptFile();

            await ExecuteProcess(exepath, args, false, delegate { }, task); // TODO: progress?
            return output;


            string getScriptFile()
            {
                var plugindir = Path.GetDirectoryName(task.Plugin.GetInstance().Path);
                var installfile = Path.GetTempFileName();
                var pythonstart = @$"python test.py ""{task.InputFile}"" ""{output}"" --tile_size 384";

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

                        {pythonstart}
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

                        {pythonstart}
                    ";
                    File.WriteAllText(installfile, script);
                }

                return installfile;
            }
        }
    }
}