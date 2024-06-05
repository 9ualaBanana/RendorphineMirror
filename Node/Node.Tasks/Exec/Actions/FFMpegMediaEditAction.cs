namespace Node.Tasks.Exec.Actions;

public abstract class FFMpegMediaEditAction<TData> : FilePluginActionInfo<TData> where TData : MediaEditInfo
{
    protected static NumberFormatInfo NumberFormat => NumberFormats.Normal;
    protected static NumberFormatInfo NumberFormatNoDecimalLimit => NumberFormats.NoDecimalLimit;

    public override ImmutableArray<PluginType> RequiredPlugins { get; } = ImmutableArray.Create(PluginType.FFmpeg);


    protected class FFmpegExecutor : ExecutorBase
    {
        public override async Task<TaskFileOutput> ExecuteUnchecked(TaskFileInput input, TData data)
        {
            var output = new TaskFileOutput(input.ResultDirectory);

            foreach (var file in input)
            {
                var ffprobe = await FFProbe.Get(file.Path, Logger);

                var launcher = new FFmpegLauncher(PluginList.GetPlugin(PluginType.FFmpeg).Path)
                {
                    ILogger = Logger,
                    ProgressSetter = ProgressSetter,
                    Input = { file.Path },
                };

                AddFilters(data, output.Files, file, ffprobe, launcher);
                await launcher.Execute();
            }

            return output;
        }

        protected virtual void AddFilters(TData data, TaskFileListList output, FileWithFormat input, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
        {
            var filters = launcher.VideoFilters;

            if (data.Crop is not null) filters.Add($"crop={(data.Crop.W - (data.Crop.W % 2)).ToString(NumberFormat)}:{(data.Crop.H - (data.Crop.H % 2)).ToString(NumberFormat)}:{data.Crop.X.ToString(NumberFormat)}:{data.Crop.Y.ToString(NumberFormat)}");
            if (data.Scale is not null) filters.Add($"scale={(data.Scale.W - (data.Scale.W % 2)).ToString(NumberFormat)}:{(data.Scale.H - (data.Scale.H % 2)).ToString(NumberFormat)}");

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
}
