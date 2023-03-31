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

        // TODO: add JPEG if fixed
        public override TaskFileFormatRequirements InputRequirements => new TaskFileFormatRequirements().Either(e => e.RequiredOne(FileFormat.Png).RequiredOne(FileFormat.Mov));
        public override TaskFileFormatRequirements OutputRequirements => new TaskFileFormatRequirements().Either(e => e.RequiredOne(FileFormat.Png).RequiredOne(FileFormat.Mov));

        protected override async Task ExecuteImpl(ReceivedTask task, RobustVideoMattingInfo data)
        {
            var input = task.InputFiles.Single();

            // TODO: support cpu or not? not easy to do actually
            var pylaunch = "python"
                + $" --device cuda"
                + $" --input-source '{input.Path}'"
                + $" --output-composition '{task.FSNewOutputFile(input.Format)}'"
                + $" --checkpoint 'models/mobilenetv3/rvm_mobilenetv3.pth'"
                + $" --variant mobilenetv3"
                + $" --seq-chunk 1"; // parallel

            await ExecutePowerShellAtWithCondaEnvAsync(task, pylaunch, false, onRead);


            void onRead(bool err, object obj)
            {
                if (err) throw new Exception(obj.ToString());
            }
        }
    }
}
