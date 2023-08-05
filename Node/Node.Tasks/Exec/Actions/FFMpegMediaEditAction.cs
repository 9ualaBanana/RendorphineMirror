namespace Node.Tasks.Exec.Actions;

public abstract class FFMpegMediaEditAction<T> : PluginAction<T> where T : MediaEditInfo
{
    protected static NumberFormatInfo NumberFormat => NumberFormats.Normal;
    protected static NumberFormatInfo NumberFormatNoDecimalLimit => NumberFormats.NoDecimalLimit;

    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.FFmpeg);

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, T data)
    {
        foreach (var input in files.InputFiles)
        {
            var ffprobe = await FFProbe.Get(input.Path, context);

            var launcher = new FFmpegLauncher(context.GetPlugin(PluginType.FFmpeg).Path)
            {
                Logger = context,
                ProgressSetter = new TaskExecutionContextProgressSetterAdapter(context),
                Input = { input.Path },
            };

            AddFilters(data, files.OutputFiles, input, ffprobe, launcher);
            await launcher.Execute();
        }
    }

    protected virtual void AddFilters(T data, TaskFileListList output, FileWithFormat input, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
    {
        var filters = launcher.VideoFilters;

        if (data.Crop is not null) filters.Add($"crop={data.Crop.W.ToString(NumberFormat)}:{data.Crop.H.ToString(NumberFormat)}:{data.Crop.X.ToString(NumberFormat)}:{data.Crop.Y.ToString(NumberFormat)}");

        var eq = new List<string>();
        if (data.Brightness is not null) eq.Add($"brightness={data.Brightness.Value.ToString(NumberFormat)}");
        if (data.Saturation is not null) eq.Add($"saturation={data.Saturation.Value.ToString(NumberFormat)}");
        if (data.Contrast is not null) eq.Add($"contrast={data.Contrast.Value.ToString(NumberFormat)}");
        if (data.Gamma is not null) eq.Add($"gamma={data.Gamma.Value.ToString(NumberFormat)}");
        if (eq.Count != 0) filters.Add($"eq={string.Join(':', eq)}");


        if (data.Hflip == true) filters.Add("hflip");
        if (data.Vflip == true) filters.Add("vflip");
        if (data.RotationRadians is not null)
        {
            var (w, h) = (ffprobe.VideoStream.Width, ffprobe.VideoStream.Height);

            var absCosRA = Math.Abs(Math.Cos(data.RotationRadians.Value));
            var absSinRA = Math.Abs(Math.Sin(data.RotationRadians.Value));
            var outw = (w * absCosRA) + (h * absSinRA);
            var outh = (w * absSinRA) + (h * absCosRA);

            filters.Add($"rotate={data.RotationRadians.Value.ToString(NumberFormat)}:ow={((int) outw).ToString(NumberFormat)}:oh={((int) outh).ToString(NumberFormat)}");
        }
    }
}