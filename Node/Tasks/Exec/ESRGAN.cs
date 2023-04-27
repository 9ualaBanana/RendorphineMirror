using Newtonsoft.Json;

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
            new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Mov }, new[] { FileFormat.Jpeg, FileFormat.Mov } };

        protected override OperationResult ValidateOutputFiles(IOTaskCheckData files, UpscaleEsrganInfo data) =>
            files.EnsureSingleInputFile()
            .Next(input => files.EnsureSingleOutputFile()
            .Next(output => TaskRequirement.EnsureSameFormat(output, input)));

        protected override async Task ExecuteImpl(ReceivedTask task, IOTaskExecutionData files, UpscaleEsrganInfo data)
        {
            foreach (var file in files.InputFiles)
            {
                var outputfile = files.OutputFiles.New().FSNewFile(file.Format);
                await upscale(file.Path, outputfile);

                if (data.X2)
                    await downscale(new FileWithFormat(file.Format, outputfile));
            }

            async Task<string> upscale(string inputfile, string outputfile)
            {
                var pylaunch = $"python "
                    + $"-u "                        // unbuffered output, for progress tracking
                    + $"test.py "                   // esrgan start file
                    + $"\"{inputfile}\" "           // input file
                    + $"\"{outputfile}\" "          // output file
                    + $"--tile_size 384 ";          // tile size; TODO: automatically determine

                await ExecutePowerShellAtWithCondaEnvAsync(task, pylaunch, false, onRead);
                return outputfile;


                void onRead(bool err, object obj)
                {
                    if (err) throw new Exception(obj.ToString());

                    var line = obj.ToString()!;
                    if (!line.StartsWith("Progress:", StringComparison.Ordinal)) return;

                    // Progress: 1/20
                    var spt = line.AsSpan("Progress: ".Length);
                    var slashidx = spt.IndexOf('/');
                    var num1 = double.Parse(spt.Slice(0, slashidx));
                    var num2 = double.Parse(spt.Slice(slashidx + 1));

                    task.Progress = num1 / num2;
                    NodeGlobalState.Instance.ExecutingTasks.TriggerValueChanged();
                }
            }
            async Task downscale(FileWithFormat file)
            {
                // ESRGAN can upscale only to x4, so for x2 we just downscale x4 by half
                task.LogInfo($"Downscaling {file.Path} to x2..");

                var outpath = Path.Combine(Init.TempDirectory(task.Id), "out." + file.Format.ToString().ToLowerInvariant());
                await FFMpegTasks.ExecuteFFMpeg(task, file, args => { args.Filtergraph.Add("scale=iw/2:ih/2"); return outpath; });
                File.Move(outpath, file.Path, true);
            }
        }


        /* unused code for splitting and combining video frames
        Task splitImages() =>
            FFMpegTasks.ExecuteFFMpeg(task, file, Path.Combine(inputdir, "out_%03d.jpg"), args =>
            {
                args.OutputFileFormat = FileFormat.Jpeg;
                args.Args.Add("-q:v", "2"); // (almost) best jpeg quality
            });
        async Task combineImages()
        {
            void cargs(FFMpegArgsHolder args)
            {
                args.FFProbe.ThrowIfNull();

                args.Args.Add("-pattern_type", "glob"); // enable glob (*) for input images
                args.Args.Add("-i", Path.Combine(inputdir, "out_*.jpg"));

                args.Args.Add("-r", args.FFProbe.VideoStream.FrameRateString); // preserve framerate

                var hasaudio = args.FFProbe.Streams.Any(s => s.CodecType == "audio");

                // copy metadata from input file to output file
                args.Args.Add("-map", "1");
                if (hasaudio) args.Args.Add("-map", "0:a");
                args.Args.Add("-map_metadata", "0");
                args.Args.Add("-map_metadata:s:v", "0:s:v");
                if (hasaudio) args.Args.Add("-map_metadata:s:a", "0:s:a");
            }

            await FFMpegTasks.ExecuteFFMpeg(task, file, outputfile, cargs);
        }
        */
    }
}