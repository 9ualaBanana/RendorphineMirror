namespace Node.Tasks.Exec.Actions;

public class FFMpegCrop
{
    public int X, Y, W, H;
}
public class FFMpegSpeed
{
    [JsonProperty("spd")]
    [Default(1d)]
    public double Speed;

    [JsonProperty("interp")]
    [Default(false)]
    public bool Interpolated;
}
public abstract class MediaEditInfo
{
    public FFMpegCrop? Crop;

    [Default(false)]
    public bool? Hflip;

    [Default(false)]
    public bool? Vflip;

    [JsonProperty("bri")]
    [Default(0), Ranged(-1, 1)]
    public double? Brightness;

    [JsonProperty("sat")]
    [Default(1), Ranged(0, 3)]
    public double? Saturation;

    [JsonProperty("con")]
    [Default(1), Ranged(-1000, 1000)]
    public double? Contrast;

    [JsonProperty("gam")]
    [Default(1), Ranged(.1, 10)]
    public double? Gamma;

    [JsonProperty("rot")]
    [Default(0), Ranged(-Math.PI * 2, Math.PI * 2)]
    public double? RotationRadians;
}

public abstract class FFMpegMediaEditAction<T> : FFMpegAction<T> where T : MediaEditInfo
{
    protected override void ConstructFFMpegArguments(ITaskExecutionContext context, T data, FFMpegArgsHolder args)
    {
        var filters = args.Filtergraph;

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
            var (w, h) = (args.FFProbe.Streams.First().Width, args.FFProbe.Streams.First().Height);

            var absCosRA = Math.Abs(Math.Cos(data.RotationRadians.Value));
            var absSinRA = Math.Abs(Math.Sin(data.RotationRadians.Value));
            var outw = w * absCosRA + h * absSinRA;
            var outh = w * absSinRA + h * absCosRA;

            filters.Add($"rotate={data.RotationRadians.Value.ToString(NumberFormat)}:ow={((int) outw).ToString(NumberFormat)}:oh={((int) outh).ToString(NumberFormat)}");
        }

        if (data.Crop is not null) filters.AddFirst($"crop={data.Crop.W.ToString(NumberFormat)}:{data.Crop.H.ToString(NumberFormat)}:{data.Crop.X.ToString(NumberFormat)}:{data.Crop.Y.ToString(NumberFormat)}");
    }
}