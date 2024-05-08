namespace Node.Tasks.Exec.Actions;

public class EditVideo : FFMpegMediaEditAction<EditVideoInfo>
{
    public override TaskAction Name => TaskAction.EditVideo;
    protected override Type ExecutorType => typeof(Executor);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Mov } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, EditVideoInfo data) =>
        files.EnsureSingleInputFile()
        .Next(input => files.EnsureSingleOutputFile()
        .Next(output =>
        {
            if (data.CutFrameAt is not (null or -1) || data.CutFramesAt is not (null or { Length: 0 }))
                return TaskRequirement.EnsureFormat(output, FileFormat.Jpeg);

            return TaskRequirement.EnsureSameFormat(output, input);
        }));


    class Executor : FFmpegExecutor
    {
        protected override void AddFilters(EditVideoInfo data, TaskFileListList output, FileWithFormat input, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
        {
            base.AddFilters(data, output, input, ffprobe, launcher);

            if (data.CutFramesAt is not (null or { Length: 0 }))
            {
                /*
                -filter_complex "
                    [0:v]split=2[o1][o2];
                    [o1]select='eq(n\,00)'[oo1];
                    [o2]select='eq(n\,33)'[oo2];
                "
                -map '[oo1]' -f image2 -qscale:v 2 -update true -frames:v 1 o1.jpg
                -map '[oo2]' -f image2 -qscale:v 2 -update true -frames:v 1 o2.jpg
                */

                var filters = string.Empty;

                // split stream
                filters += $"split={data.CutFramesAt.Length}{string.Join("", Enumerable.Range(0, data.CutFramesAt.Length).Select(i => $"[ji{i}]"))};";

                for (int i = 0; i < data.CutFramesAt.Length; i++)
                {
                    // select a frame into a [j{index}] stream
                    filters += $"[ji{i}] select= 'eq(n\\,{(int) (ffprobe.ThrowIfNull().VideoStream.FrameRate * data.CutFramesAt[i])})' [j{i}];";
                }

                launcher.VideoFilters.Add(filters);

                for (int i = 0; i < data.CutFramesAt.Length; i++)
                {
                    var part = new FFmpegLauncherOutput()
                    {
                        Codec = new JpegFFmpegCodec(),
                        Output = output.New().New(FileFormat.Jpeg, $"out_{i}").Path,
                        Args =
                        {
                            // select source stream
                            "-map", $"[j{i}]",
                        },
                    };

                    launcher.Outputs.Add(part);
                }

                return;
            }

            if (data.CutFrameAt is not (null or -1))
            {
                launcher.Outputs.Add(new FFmpegLauncherOutput()
                {
                    Codec = new JpegFFmpegCodec(),
                    Output = output.New().New(FileFormat.Jpeg).Path,
                    Args =
                    {
                        // frame position, seconds
                        "-ss", data.CutFrameAt.Value.ToString(NumberFormatNoDecimalLimit),
                    },
                });

                return;
            }

            launcher.Outputs.Add(new FFmpegLauncherOutput()
            {
                Codec = FFmpegLauncher.CodecFromStream(ffprobe.VideoStream),
                Output = output.New().New(input.Format).Path,
            });

            if (data.Speed is not null)
            {
                launcher.AudioFilters.Add($"atempo={data.Speed.Speed.ToString(NumberFormat)}");
                var fps = ffprobe.VideoStream.FrameRate;

                launcher.VideoFilters.Insert(0, new[]
                {
                    // speed up
                    $"setpts={(1d / data.Speed.Speed).ToString(NumberFormat)}*PTS",

                    // interpolation
                    !data.Speed.Interpolated ? null : $"minterpolate='mi_mode=mci:mc_mode=aobmc:vsbmc=1:fps={Math.Max(fps, fps * data.Speed.Speed).ToString(NumberFormat)}'",
                });

                // framerate
                launcher.Outputs[^1].Args.Add("-r", Math.Max(fps, fps * data.Speed.Speed).ToString(NumberFormat));
            }

            if (data.CutFromFrame is not null || data.CutToFrame is not null)
            {
                var trim = new List<string>();
                if (data.CutFromFrame is not null) trim.Add($"start_frame={data.CutFromFrame.Value.ToString(NumberFormat)}");
                if (data.CutToFrame is not null) trim.Add($"end_frame={data.CutToFrame.Value.ToString(NumberFormat)}");
                if (trim.Count != 0) launcher.VideoFilters.Add($"trim={string.Join(';', trim)}");
            }
        }
    }
}
