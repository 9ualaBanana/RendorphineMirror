using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Node.Tasks.Exec;

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
public class EditVideoInfo : MediaEditInfo
{
    [JsonProperty("spd")]
    public FFMpegSpeed? Speed;

    [JsonProperty("startFrame")]
    [Default(0d)]
    public double? StartFrame;

    [JsonProperty("endFrame")]
    public double? EndFrame;

    [JsonProperty("cutframeat")]
    public double? CutFrameAt;
}
public class EditRasterInfo : MediaEditInfo { }

public class FFMpegArgsHolder
{
    public readonly FFMpegTasks.FFProbe.FFProbeInfo? FFProbe;
    public double Rate = 1;

    public FileFormat OutputFileFormat;
    public readonly ArgList Args = new();
    public readonly OrderList<string> AudioFilers = new();
    public readonly OrderList<string> Filtergraph = new();

    public FFMpegArgsHolder(FileFormat outputFileFormat, FFMpegTasks.FFProbe.FFProbeInfo? ffprobe)
    {
        OutputFileFormat = outputFileFormat;
        FFProbe = ffprobe;
    }



    public class OrderList<T> : IEnumerable<T>
    {
        public int Count => Items.Count + ItemsLast.Count;

        readonly List<T> Items = new();
        readonly List<T> ItemsLast = new();

        public void AddFirst(T item) => Items.Insert(0, item);
        public void Add(T item) => Items.Add(item);
        public void AddLast(T item) => ItemsLast.Add(item);

        public IEnumerator<T> GetEnumerator() => Items.Concat(ItemsLast).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
public static class FFMpegTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new FFMpegEditVideo(), new FFMpegEditRaster() };
    public static async Task ExecuteFFMpeg(ReceivedTask task, FileWithFormat file, string outputfile, Action<FFMpegArgsHolder> argfunc)
    {
        var inputfile = file.Path;
        var ffprobe = await FFProbe.Get(inputfile, task) ?? throw new Exception();
        var argholder = new FFMpegArgsHolder(file.Format, ffprobe);
        argfunc(argholder);

        var args = FFMpegExec.GetFFMpegArgs(inputfile, outputfile, task, argholder);

        var duration = TimeSpan.FromSeconds(ffprobe.Format.Duration);
        task.LogInfo($"{inputfile} duration: {duration} x{argholder.Rate}");
        duration /= argholder.Rate;

        await Processes.Execute(PluginType.FFmpeg.GetInstance().Path, args, onRead, task, stderr: LogLevel.Trace);


        void onRead(bool err, string line)
        {
            // frame=  502 fps=0.0 q=29.0 size=     256kB time=00:00:14.84 bitrate= 141.3kbits/s speed=29.5x
            if (!line.StartsWith("frame=", StringComparison.Ordinal)) return;

            var spt = line.AsSpan(line.IndexOf("time=", StringComparison.Ordinal) + "time=".Length).TrimStart();
            spt = spt.Slice(0, spt.IndexOf(' '));
            if (!TimeSpan.TryParse(spt, out var time))
                time = TimeSpan.Zero;

            task.Progress = Math.Clamp(time / duration, 0, 1);
            NodeGlobalState.Instance.ExecutingTasks.TriggerValueChanged();
        }
    }


    public abstract class FFMpegActionBase<T> : InputOutputPluginAction<T>
    {
        protected static readonly NumberFormatInfo NumberFormat = new()
        {
            NumberDecimalDigits = 2,
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = string.Empty,
        };
        protected static readonly NumberFormatInfo NumberFormatNoDecimalLimit = new()
        {
            NumberDecimalDigits = 10,
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = string.Empty,
        };

        public override PluginType Type => PluginType.FFmpeg;

        protected delegate void ConstructFFMpegArgumentsDelegate(ReceivedTask task, T data, in FFMpegArgsHolder args);
        protected static async Task ExecuteFFMpeg(ReceivedTask task, T data, FileWithFormat file, TaskFileList outfiles, ConstructFFMpegArgumentsDelegate argfunc)
        {
            var inputfile = file.Path;
            var ffprobe = await FFProbe.Get(inputfile, task) ?? throw new Exception();
            var argholder = new FFMpegArgsHolder(file.Format, ffprobe);

            await FFMpegTasks.ExecuteFFMpeg(task, file, outfiles.FSNewFile(argholder.OutputFileFormat), args => argfunc(task, data, args));
        }
    }
    public abstract class FFMpegAction<T> : FFMpegActionBase<T>
    {
        protected sealed override async Task ExecuteImpl(ReceivedTask task, IOTaskExecutionData files, T data)
        {
            foreach (var file in files.InputFiles)
                await ExecuteFFMpeg(task, data, file, files.OutputFiles, ConstructFFMpegArguments);
        }

        protected abstract void ConstructFFMpegArguments(ReceivedTask task, T data, in FFMpegArgsHolder args);
    }

    abstract class FFMpegMediaEditAction<T> : FFMpegAction<T> where T : MediaEditInfo
    {
        protected override void ConstructFFMpegArguments(ReceivedTask task, T data, in FFMpegArgsHolder args)
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
                if (args.FFProbe is null) task.ThrowFailed("Could not execute ffprobe to correctly rotate the image");

                var (w, h) = (args.FFProbe.Streams.First().Width, args.FFProbe.Streams.First().Height);

                var absCosRA = Math.Abs(Math.Cos(data.RotationRadians.Value));
                var absSinRA = Math.Abs(Math.Sin(data.RotationRadians.Value));
                var outw = w * absCosRA + h * absSinRA;
                var outh = w * absSinRA + h * absCosRA;

                filters.Add($"rotate={data.RotationRadians.Value.ToString(NumberFormat)}:ow={outw.ToString(NumberFormat)}:oh={outh.ToString(NumberFormat)}");
            }

            if (data.Crop is not null) filters.AddFirst($"crop={data.Crop.W.ToString(NumberFormat)}:{data.Crop.H.ToString(NumberFormat)}:{data.Crop.X.ToString(NumberFormat)}:{data.Crop.Y.ToString(NumberFormat)}");
        }
    }
    class FFMpegEditVideo : FFMpegMediaEditAction<EditVideoInfo>
    {
        public override TaskAction Name => TaskAction.EditVideo;

        public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
            new[] { new[] { FileFormat.Mov } };

        protected override OperationResult ValidateOutputFiles(IOTaskCheckData files, EditVideoInfo data) =>
            files.EnsureSingleInputFile()
            .Next(input => files.EnsureSingleOutputFile()
            .Next(output =>
            {
                if (data.CutFrameAt is not (null or -1))
                    return TaskRequirement.EnsureFormat(output, FileFormat.Jpeg);

                return TaskRequirement.EnsureSameFormat(output, input);
            }));

        protected override void ConstructFFMpegArguments(ReceivedTask task, EditVideoInfo data, in FFMpegArgsHolder args)
        {
            var filters = args.Filtergraph;
            base.ConstructFFMpegArguments(task, data, args);

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

                var fps = args.FFProbe?.VideoStream.FrameRate ?? 60;
                args.Args.Add("-r", Math.Max(fps, (fps * data.Speed.Speed)).ToString(NumberFormat));
                if (data.Speed.Interpolated) filters.Add($"minterpolate='mi_mode=mci:mc_mode=aobmc:vsbmc=1:fps={Math.Max(fps, (fps * data.Speed.Speed)).ToString(NumberFormat)}'");
            }

            var trim = new List<string>();
            if (data.StartFrame is not null) trim.Add($"start_frame={data.StartFrame.Value.ToString(NumberFormat)}");
            if (data.EndFrame is not null) trim.Add($"end_frame={data.EndFrame.Value.ToString(NumberFormat)}");
            if (trim.Count != 0) filters.AddFirst($"trim={string.Join(';', trim)}");
        }
    }
    class FFMpegEditRaster : FFMpegMediaEditAction<EditRasterInfo>
    {
        public override TaskAction Name => TaskAction.EditRaster;

        public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
            new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Png } };

        protected override OperationResult ValidateOutputFiles(IOTaskCheckData files, EditRasterInfo data) =>
            files.EnsureSingleInputFile()
            .Next(input => files.EnsureSingleOutputFile()
            .Next(output => TaskRequirement.EnsureSameFormat(output, input)));
    }


    public static class FFProbe
    {
        public static async Task<FFProbeInfo> Get(string file, ILoggable? logobj)
        {
            var ffprobe = File.Exists("/bin/ffprobe") ? "/bin/ffprobe" : "assets/ffprobe.exe";

            var args = $"-hide_banner -v quiet -print_format json -show_streams -show_format \"{file}\"";
            logobj?.LogInfo($"Starting {ffprobe} {args}");


            var proc = Process.Start(new ProcessStartInfo(ffprobe, args)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
            if (proc is null) throw new Exception("Could not start ffprobe");

            var str = await proc.StandardOutput.ReadToEndAsync();
            return JsonConvert.DeserializeObject<FFProbeInfo>(str) ?? throw new Exception($"Could not parse ffprobe output: {str}");
        }


        public record FFProbeInfo(ImmutableArray<FFProbeStreamInfo> Streams, FFProbeFormatInfo Format)
        {
            // die if there are multiple video streams
            public FFProbeStreamInfo VideoStream
            {
                get
                {
                    try { return Streams.Single(x => x.CodecType.Equals("video", StringComparison.OrdinalIgnoreCase) && x.FrameRate < 10000); }
                    catch (Exception ex) { throw new Exception($"Found more than one video stream: {string.Join("; ", Streams)}", ex); }
                }
            }
        };

        public record FFProbeStreamInfo(
            int Width,
            int Height,
            [JsonProperty("codec_name")] string CodecName,
            [JsonProperty("codec_type")] string CodecType,
            [JsonProperty("r_frame_rate")] string FrameRateString
        )
        {
            public double FrameRate => double.Parse(FrameRateString.Split('/')[0]) / double.Parse(FrameRateString.Split('/')[1]);
        }

        public record FFProbeFormatInfo(
            double Duration
        );
    }
}