using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Node.Tasks.Exec;

public class UpscaleEsrganInfo
{
    [JsonProperty("x2")]
    [Default(false)]
    public bool X2;
}
public static class EsrganTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new UpscaleEsrgan() };


    class UpscaleEsrgan : InputOutputPluginAction<UpscaleEsrganInfo>
    {
        public override TaskAction Name => TaskAction.EsrganUpscale;
        public override PluginType Type => PluginType.Esrgan;

        public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
            new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Mov } };

        protected override OperationResult ValidateOutputFiles(IOTaskCheckData files, UpscaleEsrganInfo data) =>
            files.EnsureSingleInputFile()
            .Next(input => files.EnsureSingleOutputFile()
            .Next(output => TaskRequirement.EnsureSameFormat(output, input)));

        protected override async Task ExecuteImpl(ReceivedTask task, IOTaskExecutionData files, UpscaleEsrganInfo data)
        {
            var inputfile = files.InputFiles.Single().Path;
            var outputfile = files.OutputFiles.FSNewFile(FileFormat.Jpeg);

            var pylaunch = $"python "
                + $"-u "                        // unbuffered output, for progress tracking
                + $"test.py "                   // esrgan start file
                + $"\"{inputfile}\" "           // input file
                + $"\"{outputfile}\" "          // output file
                + $"--tile_size 384 ";          // tile size; TODO: automatically determine

            await ExecutePowerShellAtWithCondaEnvAsync(task, pylaunch, false, onRead);

            if (data.X2)
            {
                task.LogInfo("Downscaling the result to x2..");

                using var image = Image.Load<Rgba32>(outputfile);
                image.Mutate(img => img.Resize(image.Width / 2, image.Height / 2));
                await image.SaveAsJpegAsync(outputfile);

                /*
                    or use ffmpeg, aka:

                    var downscale = "";
                    if (data.X2)
                    {
                        var tempfile = task.GetTempFileName("jpg");
                        downscale = $@"
                            {PluginType.FFmpeg.GetInstance().Path.Replace(" ", "' '")} -i '{outputfile}' -filter_complex 'scale=iw/2:ih/2' '{tempfile}'
                            mv '{tempfile}' '{outputfile}'
                        ";
                    }
                */
            }


            void onRead(bool err, object obj)
            {
                if (err) throw new Exception(obj.ToString());

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
        }
    }
}