namespace Node.Tasks.Exec;

public class UpscaleEsrganInfo { }
public static class EsrganTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new UpscaleEsrgan() };


    class UpscaleEsrgan : InputOutputPluginAction<UpscaleEsrganInfo>
    {
        public override string Name => "EsrganUpscale";
        public override PluginType Type => PluginType.Python_Esrgan;
        public override FileFormat InputFileFormat => FileFormat.Jpeg;

        protected override async Task ExecuteImpl(ReceivedTask task, UpscaleEsrganInfo data)
        {
            var inputfile = task.FSInputFile();
            var outputfile = task.FSNewOutputFile(InputFileFormat.ToString().ToLowerInvariant());

            await Task.Run(() => ExecutePowerShell(getScript(), false, onRead, task));


            void onRead(bool err, object obj)
            {
                var line = obj.ToString()!;
                if (!line.StartsWith("Progress:")) return;

                // Progress: 1/20
                var spt = line.AsSpan("Progress: ".Length);
                var slashidx = spt.IndexOf('/');
                var num1 = double.Parse(spt.Slice(0, slashidx));
                var num2 = double.Parse(spt.Slice(slashidx + 1));

                task.Progress = num1 / num2;
                NodeGlobalState.Instance.ExecutingTasks.TriggerValueChanged();
            }
            string getScript()
            {
                var plugindir = Path.GetFullPath(Path.GetDirectoryName(task.GetPlugin().GetInstance().Path)!);
                var pythonpath = PluginType.Python.GetInstance().Path.Replace(" ", "' '");


                var pythonstart = "python ";

                // unbuffered output, for progress tracking
                pythonstart += "-u ";

                // esrgan start file
                pythonstart += "test.py ";

                // input file
                pythonstart += $"\"{inputfile}\" ";

                // output file
                pythonstart += $"\"{outputfile}\" ";

                // tile size; TODO: automatically determine
                pythonstart += "--tile_size 384 ";


                var script = $@"
                    Set-Location ""{plugindir}""
                    {pythonpath} -m venv venv
                    if (Test-Path -Path ./venv/bin/activate.ps1 -PathType Leaf)
                    {{ ./venv/bin/activate }}
                    if (Test-Path -Path ./venv/Scripts/activate.ps1 -PathType Leaf)
                    {{ ./venv/Scripts/activate }}

                    pip3 install -r ./installation/requirements.txt
                    foreach($line in Get-Content ./installation/precommands.txt){{
                        Invoke-Expression $line
                    }}

                    {pythonstart}
                ";

                return script;
            }
        }
    }
}