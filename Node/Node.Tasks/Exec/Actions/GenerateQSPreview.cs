using SixLabors.ImageSharp.Formats.Jpeg;

namespace Node.Tasks.Exec.Actions;

public class QSPreviewInfo { }

public class GenerateQSPreview : FFMpegActionBase<QSPreviewInfo>
{
    const int MaximumPixels = 129600;
    const double MaxByWidth = .6;
    const double MaxByHeight = .3;

    double? CachedWatermarkAspectRatio;
    Image<Rgba32>? AdressImage, LogoImage;

    public override TaskAction Name => TaskAction.GenerateQSPreview;

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Jpeg, FileFormat.Mov } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, QSPreviewInfo data) =>
        files.EnsureSameFormats();

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, QSPreviewInfo data)
    {
        var outfiles = files.OutputFiles.New();
        foreach (var file in files.InputFiles)
        {
            if (file.Format == FileFormat.Jpeg)
                ProcessJpeg(file.Path, outfiles.New(FileFormat.Jpeg).Path);
            else if (file.Format == FileFormat.Mov)
                await ExecuteFFMpeg(context, data, file, outfiles, ConstructFFMpegArguments);
        }
    }


    void ProcessJpeg(string input, string output)
    {
        AdressImage ??= Image.Load<Rgba32>("assets/qswatermark/adress.png");
        LogoImage ??= Image.Load<Rgba32>("assets/qswatermark/logo.png");

        using var image = Image.Load<Rgba32>(input);
        var (outwidth, outheight) = Scale(image.Width, image.Height);

        image.Mutate(ctx => ctx
            .Resize(outwidth, outheight)
            .Resize(new ResizeOptions()
            {
                Size = new(outwidth, outheight + 20),
                PadColor = Color.FromRgb(25, 21, 52),
                Mode = ResizeMode.Pad,
                Sampler = KnownResamplers.NearestNeighbor,
                Position = AnchorPositionMode.TopLeft,
            })
            .DrawImage(AdressImage, new Point(outwidth - 10 - AdressImage.Width, outheight + 20 / 2 - AdressImage.Height / 2), 1)
            .DrawImage(LogoImage, new Point(10, outheight + 20 / 2 - LogoImage.Height / 2), 1)
        );

        image.SaveAsJpeg(output, new JpegEncoder() { Quality = 90 });
    }

    void ConstructFFMpegArguments(ITaskExecutionContext context, QSPreviewInfo data, FFMpegArgsHolder args)
    {
        var watermarkFile = Path.GetFullPath("assets/qswatermark/watermark.png");

        var video = args.FFProbe.VideoStream;
        var (outwidth, outheight) = Scale(video.Width, video.Height);


        if (CachedWatermarkAspectRatio is null)
        {
            using var water = Image.Load<Rgba32>(watermarkFile);
            CachedWatermarkAspectRatio = water.Width / water.Height;
        }

        var waterw = outwidth * MaxByWidth;
        var waterh = waterw / CachedWatermarkAspectRatio.Value;
        if (waterh > outheight * MaxByHeight)
        {
            waterh = outheight * MaxByHeight;
            waterw = waterh * CachedWatermarkAspectRatio.Value;
        }
        waterw += waterw % 2;
        waterh += waterh % 2;


        // input watermark file
        args.Args.Add("-i", watermarkFile);

        // no audio
        args.Args.Add("-an");

        // enable streaming
        args.Args.Add("-movflags", "faststart");

        // decrease bitrate
        args.Args.Add("-cq:v", "25");


        var graph = "";

        // scale video to maximum of MaximumPixels
        graph += $"[0] scale= w={(int) outwidth}:h={(int) outheight} [v];";

        // scale watermark
        graph += $"[1] scale= w={(int) waterw}:h={(int) waterh},";

        // set watermark transparency to 60%
        graph += $"format=rgba, colorchannelmixer=aa=0.6 [o];";

        // overlay watermark
        graph += $"[v][o] overlay= (main_w-overlay_w)/2:((main_h-main_h/3)-overlay_h/2):format=auto,";

        // set the color format
        graph += "format= yuv420p";

        args.Filtergraph.AddLast(graph);
    }

    static (int width, int height) Scale(int width, int height)
    {
        var pixels = width * height;
        var outwidth = width;
        if (MaximumPixels < pixels)
        {
            var scale = MaximumPixels / (double) pixels;
            outwidth = (int) (width * Math.Sqrt(scale));
        }

        var outheight = (int) ((double) height / width * outwidth);

        outwidth += outwidth % 2;
        outheight += outheight % 2;
        return (outwidth, outheight);
    }
}