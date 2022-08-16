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
            var exepath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd" : "/bin/sh";

            var args = "";

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // hide window
                args += "/c start /min \"\" ";

                // bypass powershell restrictions
                args += "powershell -ExecutionPolicy Bypass -File ";
            }

            args += getScriptFile();

            await ExecuteProcess(exepath, args, false, onRead, task);
            return output;


            void onRead(bool err, string line)
            {
                if (!line.StartsWith("Progress:")) return;

                // Progress: 1/20
                var spt = line.AsSpan("Progress: ".Length);
                var slashidx = spt.IndexOf('/');
                var num1 = double.Parse(spt.Slice(0, slashidx));
                var num2 = double.Parse(spt.Slice(slashidx + 1));

                task.Progress = num1 / num2;
                NodeGlobalState.Instance.ExecutingTasks.TriggerValueChanged();
            }
            string getScriptFile()
            {
                var plugindir = Path.GetDirectoryName(task.GetPlugin().GetInstance().Path);
                var installfile = Path.Combine(Path.GetDirectoryName(output)!, "p.ps1");
                var pythonpath = PluginType.Python.GetInstance().Path;

                // i hate powershell
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    pythonpath = pythonpath.Replace(" ", "' '");
                else pythonpath = pythonpath.Replace(" ", @"\ ");


                var pythonstart = "python ";

                // unbuffered output, for progress tracking
                pythonstart += "-u ";

                // esrgan start file
                pythonstart += "test.py ";

                // input file
                pythonstart += $"\"{task.InputFile}\" ";

                // output file
                pythonstart += $"\"{output}\" ";

                // tile size; TODO: automatically determine
                pythonstart += "--tile_size 384 ";



                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var script = $@"
                        Set-Location ""{plugindir}""
                        {pythonpath} -m venv venv
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
                        {pythonpath} -m venv venv
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