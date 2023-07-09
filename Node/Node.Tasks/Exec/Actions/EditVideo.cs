namespace Node.Tasks.Exec.Actions;

public class EditVideo : FFMpegMediaEditAction<EditVideoInfo>
{
    public override TaskAction Name => TaskAction.EditVideo;

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

    protected override void ConstructFFMpegArguments(ITaskExecutionContext context, EditVideoInfo data, FFMpegArgsHolder args)
    {
        var filters = args.Filtergraph;
        base.ConstructFFMpegArguments(context, data, args);

        if (data.CutFramesAt is not (null or { Length: 0 }))
        {
            args.OutputFileFormat = FileFormat.Jpeg;
            args.OutputFileName = "output_%3d.jpeg";

            // i dont know why but this is needed
            args.Args.Add("-vsync", "0");

            // select only needed frames
            var frames = data.CutFramesAt.Select(f => $@"eq(n\,{(int) (args.FFProbe.ThrowIfNull().VideoStream.FrameRate * f)})");
            args.Filtergraph.Add($@"select='{string.Join('+', frames)}'");

            return;
        }
        if (data.CutFrameAt is not (null or -1))
        {
            args.OutputFileFormat = FileFormat.Jpeg;

            // frame position, seconds
            args.Args.Add("-ss", data.CutFrameAt.Value.ToString(NumberFormatNoDecimalLimit));

            // cut a single frame
            args.Args.Add("-frames:v", "1");
            return;
        }

        if (data.Speed is not null)
        {
            args.Rate = data.Speed.Speed;
            filters.Add($"setpts={(1d / data.Speed.Speed).ToString(NumberFormat)}*PTS");
            args.AudioFilers.Add($"atempo={data.Speed.Speed.ToString(NumberFormat)}");

            var fps = args.FFProbe.VideoStream.FrameRate;
            args.Args.Add("-r", Math.Max(fps, fps * data.Speed.Speed).ToString(NumberFormat));
            if (data.Speed.Interpolated) filters.Add($"minterpolate='mi_mode=mci:mc_mode=aobmc:vsbmc=1:fps={Math.Max(fps, (fps * data.Speed.Speed)).ToString(NumberFormat)}'");
        }

        var trim = new List<string>();
        if (data.StartFrame is not null) trim.Add($"start_frame={data.StartFrame.Value.ToString(NumberFormat)}");
        if (data.EndFrame is not null) trim.Add($"end_frame={data.EndFrame.Value.ToString(NumberFormat)}");
        if (trim.Count != 0) filters.AddFirst($"trim={string.Join(';', trim)}");
    }
}