using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public record RVMColor(byte R, byte G, byte B);
public class RobustVideoMattingInfo
{
    [JsonProperty("color")]
    public RVMColor? Color;
}
public static class RobustVideoMatting
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new GreenscreenBackgroundEsrgan() };


    // micromamba install pytorch==1.9.0=*cuda* torchvision==0.10.0 tqdm==4.61.1 pims==0.5 cuda-toolkit -c nvidia/label/cuda-11.8.0 -c torch -c conda-forge
    class GreenscreenBackgroundEsrgan : InputOutputPluginAction<RobustVideoMattingInfo>
    {
        public override TaskAction Name => TaskAction.GreenscreenBackground;
        public override PluginType Type => PluginType.RobustVideoMatting;

        public override TaskFileFormatRequirements InputRequirements =>
            new TaskFileFormatRequirements().Either(e => e.RequiredOne(FileFormat.Mov).RequiredOne(FileFormat.Png).RequiredOne(FileFormat.Jpeg));
        public override TaskFileFormatRequirements OutputRequirements =>
            new TaskFileFormatRequirements().Either(e => e.RequiredOne(FileFormat.Mov).RequiredOne(FileFormat.Png).RequiredOne(FileFormat.Jpeg));

        protected override async Task ExecuteImpl(ReceivedTask task, RobustVideoMattingInfo data)
        {
            var input = task.InputFiles.MaxBy(f => f.Format).ThrowIfNull("Could not find input file");

            var outputformat = input.Format;
            if (input.Format == FileFormat.Jpeg && data.Color is null)
                outputformat = FileFormat.Png;

            // TODO: support cpu or not? not easy to do actually
            var pylaunch = "python"
                + $" -u" // unbuffered output, for progress tracking
                + $" inference.py"
                + $" --device cuda"
                + $" --input-source '{input.Path}'"
                + $" --output-composition '{task.FSNewOutputFile(outputformat)}'"
                + $" --checkpoint 'models/mobilenetv3/rvm_mobilenetv3.pth'"
                + $" --variant mobilenetv3"
                + $" --output-type file"
                + $" --seq-chunk 1"; // parallel
            if (data.Color is not null)
                pylaunch += $" --background-color {data.Color.R} {data.Color.G} {data.Color.B}";

            await ExecutePowerShellAtWithCondaEnvAsync(task, pylaunch, false, onRead);


            void onRead(bool err, object obj)
            {
                if (err) throw new Exception(obj.ToString());

                // 1/1024
                var str = obj.ToString();
                if (str is null) return;

                var spt = str.Split('/');
                if (spt.Length != 2) return;

                if (!int.TryParse(spt[0], out var left))
                    return;
                if (!int.TryParse(spt[1], out var right))
                    return;

                task.Progress = (float) left / right;
            }
        }
    }
}
