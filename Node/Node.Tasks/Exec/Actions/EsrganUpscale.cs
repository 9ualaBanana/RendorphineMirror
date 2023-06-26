namespace Node.Tasks.Exec.Actions;

public class UpscaleEsrganInfo
{
    [JsonProperty("x2")]
    [Default(false)]
    public bool X2;
}

public class EsrganUpscale : PluginAction<UpscaleEsrganInfo>
{
    public override TaskAction Name => TaskAction.EsrganUpscale;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.Esrgan);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Mov }, new[] { FileFormat.Png } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, UpscaleEsrganInfo data) =>
        files.EnsureSingleInputFile()
        .Next(input => files.EnsureSingleOutputFile()
        .Next(output => TaskRequirement.EnsureSameFormat(output, input)));

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, UpscaleEsrganInfo data)
    {
        foreach (var file in files.InputFiles)
        {
            var fs = files.OutputFiles.New();
            var outputfile = fs.New(file.Format);
            await upscale(file.Path, outputfile.Path);

            if (data.X2)
            {
                files.OutputFiles.Remove(fs);
                await downscale(outputfile);
            }
        }


        async Task upscale(string inputfile, string outputfile)
        {
            var pylaunch = $"python "
                + $"-u "                        // unbuffered output, for progress tracking
                + $"test.py "                   // esrgan start file
                + $"\"{inputfile}\" "           // input file
                + $"\"{outputfile}\" "          // output file
                + $"--tile_size 384 ";          // tile size; TODO: automatically determine

            await CondaInvoker.ExecutePowerShellAtWithCondaEnvAsync(context, PluginType.Esrgan, pylaunch, onRead, context);


            void onRead(bool err, object obj)
            {
                if (err)
                {
                    /*
                    Upscaling prores will show an error
                    [prores @ 0000026CC5A555C0] Specified pixel format yuv420p is invalid or not supported
                    [ERROR:0@4.856] global cap_ffmpeg_impl.hpp:3049 CvVideoWriter_FFMPEG::open Could not open codec prores, error: Unspecified error (-22)
                    [ERROR:0@4.856] global cap_ffmpeg_impl.hpp:3066 CvVideoWriter_FFMPEG::open VIDEOIO/FFMPEG: Failed to initialize VideoWriter

                    but the file still will be succesfully upscaled, so we skip those errors
                    BUT, only on windows. Linux does not produce the result for some reason.

                    Though we won't specifically check for windows since linux shows another error that we just don't skip:
                    [ERROR:0@0.916] global cap.cpp:595 open VIDEOIO(CV_IMAGES): raised OpenCV exception:
                    OpenCV(4.7.0) /home/conda/feedstock_root/build_artifacts/libopencv_1675729965945/work/modules/videoio/src/cap_images.cpp:253: error: (-5:Bad argument) CAP_IMAGES: can't find starting number (in the name of file): /temp/file.mov in function 'icvExtractPattern'
                    */

                    var errstr = obj.ToString() ?? "";
                    var skipthrow =
                        (errstr.Contains("Specified pixel format", StringComparison.Ordinal) && errstr.Contains("is invalid or not supported", StringComparison.Ordinal))
                        || errstr.Contains("Could not open codec prores")
                        || errstr.Contains("VIDEOIO/FFMPEG");

                    if (!skipthrow)
                        throw new Exception(errstr);
                }

                var line = obj.ToString()!;
                if (!line.StartsWith("Progress:", StringComparison.Ordinal)) return;

                // Progress: 1/20
                var spt = line.AsSpan("Progress: ".Length);
                var slashidx = spt.IndexOf('/');
                var num1 = double.Parse(spt.Slice(0, slashidx));
                var num2 = double.Parse(spt.Slice(slashidx + 1));

                context.SetProgress(Math.Min(num1 / num2, .99));
            }
        }
        async Task downscale(FileWithFormat file)
        {
            // ESRGAN can upscale only to x4, so for x2 we just downscale x4 by half
            context.LogInfo($"Downscaling {file.Path} to x2..");

            var outpath = Path.Combine(files.OutputFiles.Directory, "out_downscaled" + file.Format.AsExtension());
            await FFMpegExec.ExecuteFFMpeg(context, file, files.OutputFiles, args => { args.Filtergraph.Add("scale=iw/2:ih/2"); return outpath; });
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