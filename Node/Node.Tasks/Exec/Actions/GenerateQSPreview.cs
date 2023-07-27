using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Node.Tasks.Exec.Actions;

public class GenerateQSPreview : PluginAction<QSPreviewInfo>
{
    const float WatermarkTransparency = .7f;
    const int MaximumPixels = 129600;

    Image<Rgba32>? LogoImage, WatermarkImage, QwertyLogoImage;

    public override TaskAction Name => TaskAction.GenerateQSPreview;

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Mov }, new[] { FileFormat.Jpeg, FileFormat.Mov } };

    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.FFmpeg);

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, QSPreviewInfo data)
    {
        var jpeg = files.InputFiles.TryFirst(FileFormat.Jpeg) is not null;
        var mov = files.InputFiles.TryFirst(FileFormat.Mov) is not null;

        var format = new List<FileFormat>();
        if (jpeg)
        {
            format.Add(FileFormat.Jpeg);
            if (!mov) format.Add(FileFormat.Jpeg);
        }
        if (mov)
            format.Add(FileFormat.Mov);

        return files.EnsureOutputFormats(new[] { format });
    }

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, QSPreviewInfo data)
    {
        var id = data.Qid;
        using var qr = await GenerateQR($"https://qwertystock.com/item?id={id}");

        var outfiles = files.OutputFiles.New();

        var jpeg = files.InputFiles.TryFirst(FileFormat.Jpeg);
        var mov = files.InputFiles.TryFirst(FileFormat.Mov);

        if (jpeg is not null)
        {
            await ProcessPreviewFooter(id, jpeg.Path, outfiles.New(FileFormat.Jpeg, "pj_footer").Path);

            if (mov is null)
                await ProcessPreviewQr(jpeg.Path, qr, outfiles.New(FileFormat.Jpeg, "pj_qr").Path);
        }

        if (mov is not null)
        {
            using var _ = Directories.TempFile(out var qrfile, "qsprveiew_qr");
            await qr.SaveAsPngAsync(qrfile);

            await ProcessVideoPreview(context, mov.Path, qrfile, qr, outfiles.New(FileFormat.Mov, "pv1").Path);
        }
    }


    async Task<Image<Rgba32>> GenerateQR(string data) =>
        QRGenerator.Generate(data, QwertyLogoImage ??= await Image.LoadAsync<Rgba32>("assets/qswatermark/qwerty_logo.png"), 1, 1, .2f);


    async Task ProcessPreviewQr(string input, Image qr, string output)
    {
        const double maxByWidth = .6;
        const double maxByHeight = .3;

        WatermarkImage ??= await Image.LoadAsync<Rgba32>("assets/qswatermark/watermark.png");

        using var image = await Image.LoadAsync<Rgba32>(input);
        var (outwidth, outheight) = Scale(image.Width, image.Height, MaximumPixels);

        var ww = outwidth * maxByWidth;
        var wh = ww / (WatermarkImage.Width / WatermarkImage.Height);
        if (outwidth * maxByWidth / (WatermarkImage.Width / WatermarkImage.Height) > outheight * maxByHeight)
        {
            wh = outheight * maxByHeight;
            ww = wh * (WatermarkImage.Width / WatermarkImage.Height);
        }

        using var watermark = WatermarkImage.Clone(ctx => ctx.Resize((int) ww, (int) wh));
        image.Mutate(ctx => ctx
            .Resize(outwidth, outheight)
            .DrawImage(watermark, new Point((outwidth - (int) ww) / 2, (int) ((outheight / 3f * 2) - (wh / 2))), WatermarkTransparency)
            .DrawImage(qr, new Point(outwidth - qr.Width - 5, outheight - qr.Height - 5), WatermarkTransparency)
        );

        await image.SaveAsJpegAsync(output, new JpegEncoder() { Quality = 90 });
    }

    async Task ProcessPreviewFooter(string id, string input, string output)
    {
        LogoImage ??= await Image.LoadAsync<Rgba32>("assets/qswatermark/logo.png");

        using var image = await Image.LoadAsync<Rgba32>(input);
        var (outwidth, outheight) = Scale(image.Width, image.Height, MaximumPixels);

        var font = new FontCollection().Add("assets/qswatermark/Overpass-Bold.ttf");

        const int footerh = 20;
        const int padding = 6;

        image.Mutate(ctx => ctx
            .Resize(outwidth, outheight)
            .Resize(new ResizeOptions()
            {
                Size = new(outwidth, outheight + footerh),
                PadColor = Color.FromRgb(25, 21, 52),
                Mode = ResizeMode.Pad,
                Sampler = KnownResamplers.NearestNeighbor,
                Position = AnchorPositionMode.TopLeft,
            })
            .DrawImage(LogoImage, new Point(padding, outheight + (footerh / 2) - (LogoImage.Height / 2)), 1)
            .DrawText(new TextOptions(new Font(font, 8f))
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Origin = new System.Numerics.Vector2(outwidth - padding, outheight + (footerh / 2))
            }, $"ID: {id}", Color.FromRgb(0x8a, 0x8d, 0xa0))
        );

        await image.SaveAsJpegAsync(output, new JpegEncoder() { Quality = 90 });
    }

    async Task ProcessVideoPreview(ITaskExecutionContext context, string input, string qr, Image qrimage, string output)
    {
        await (await buildLauncher()).Execute();


        async Task<FFmpegLauncher> buildLauncher()
        {
            const double fadeTime = 0.3;
            const int watermarkWaitTime = 6;

            var qrsize = qrimage.Width;
            const double maxsizebywidthH = 0.45;
            const double maxsizebywidthV = 0.6;
            const double maxsizebyheight = 0.3;

            var ffprobe = await FFProbe.Get(input, context);

            var vertical = ffprobe.VideoStream.Width < ffprobe.VideoStream.Height;
            var maxsizebywidth = vertical ? maxsizebywidthV : maxsizebywidthH;

            var wshift = Random.Shared.NextDouble() * (Math.PI * 2);
            var xshift = .97;
            var yshift = .6777;

            var launcher = new FFmpegLauncher(context.GetPlugin(PluginType.FFmpeg).Path)
            {
                Logger = context,
                ProgressSetter = new TaskExecutionContextProgressSetterAdapter(context),
                Input =
                {
                    input,
                    new FFmpegLauncherInput("assets/qswatermark/watermark.png") { Args = { "-loop", "1" } },
                    qr,
                },
                Outputs =
                {
                    new FFmpegLauncherOutput()
                    {
                        Codec = new H264NvencFFmpegCodec() { Bitrate = new BitrateData.Variable("31") },
                        Output = output,
                        Args =
                        {
                            // no audio
                            "-an",

                            // move moov to start for streaming support
                            "-movflags", "faststart",

                            // pixel format compatible with h264
                            "-pix_fmt", "yuv420p",
                        },
                    },
                },
                VideoFilters =
                {
                    new FFmpegFilter.FilterList()
                    {
                        new FFmpegFilter.Block(ImmutableArray.Create("0"), ImmutableArray.Create("vid"))
                        {
                            new FFmpegFilter.Filter("scale")
                                .Set("force_divisible_by", 2)
                                .Set("force_original_aspect_ratio", "decrease")
                                .Set("w", $"in_w * sqrt({MaximumPixels} / (in_w * in_h))")
                                .Set("h", "in_h / in_w * out_w"),
                        },
                        new FFmpegFilter.Block(ImmutableArray.Create("1", "vid"), ImmutableArray.Create("txt", "vid"))
                        {
                            new FFmpegFilter.Filter("scale2ref")
                                .Set("w", $"""
                                    if(gte((iw * {maxsizebywidth} / main_a)\, ih * {maxsizebyheight})\,
                                        (ih * {maxsizebyheight}) * main_a\,
                                        iw * {maxsizebywidth}
                                    )
                                    """)
                                .Set("h", $"""
                                    if(gte((iw * {maxsizebywidth} / main_a)\, ih * {maxsizebyheight})\,
                                        ih * {maxsizebyheight}\,
                                        (iw * {maxsizebywidth}) / main_a
                                    )
                                    """),
                        },
                        !vertical
                            ? new FFmpegFilter.Block(ImmutableArray.Create("txt"), ImmutableArray.Create("txt"))
                            {
                                new FFmpegFilter.Filter("format")
                                    .Add("rgba"),
                                new FFmpegFilter.Filter("pad")
                                    .Set("x", qrsize)
                                    .Set("y", "iw/2 - oh/2")
                                    .Set("w", $"iw+{qrsize}")
                                    .Set("h", $@"max(ih\, {qrsize})")
                                    .Set("color", "#00000000"),
                            }
                            : new FFmpegFilter.Block(ImmutableArray.Create("txt"), ImmutableArray.Create("txt"))
                            {
                                new FFmpegFilter.Filter("format")
                                    .Add("rgba"),
                                new FFmpegFilter.Filter("pad")
                                    .Set("y", qrsize)
                                    .Set("h", $"ih+{qrsize}")
                                    .Set("color", "#00000000"),
                            },
                        new FFmpegFilter.Block(ImmutableArray.Create("txt", "2"), ImmutableArray.Create("wtr"))
                        {
                            !vertical
                                ? new FFmpegFilter.Filter("overlay")
                                    .Set("y", @"(main_h - overlay_h) / 2")
                                : new FFmpegFilter.Filter("overlay")
                                    .Set("x", @"(main_w - overlay_w) / 2"),
                            new FFmpegFilter.Filter("colorchannelmixer")
                                .Set("aa", WatermarkTransparency),
                            new FFmpegFilter.Filter("format")
                                .Add("rgba"),
                            new FFmpegFilter.Filter("fade")
                                .Set("t", "in")
                                .Set("d", fadeTime)
                                .Set("st", 0),
                            new FFmpegFilter.Filter("fade")
                                .Set("t", "out")
                                .Set("d", fadeTime)
                                .Set("st", watermarkWaitTime - fadeTime),
                            new FFmpegFilter.Filter("trim")
                                .Set("duration", watermarkWaitTime),
                            new FFmpegFilter.Filter("loop")
                                .Add(-1)
                                .Set("size", 9999),
                        },
                        new FFmpegFilter.Block(ImmutableArray.Create("vid", "wtr"), ImmutableArray<string>.Empty)
                        {
                            new FFmpegFilter.Filter("overlay")
                                .Set("x", $@"((cos({wshift} + floor(t / {watermarkWaitTime}) * {1.25 * Math.PI}) / 2 + 0.5) * {xshift} + {(1 - xshift) / 2}) * (main_w - overlay_w)")
                                .Set("y", $@"((sin({wshift} + floor(t / {watermarkWaitTime}) * {1.25 * Math.PI}) / 2 + 0.5) * {yshift} + {(1 - yshift) / 2}) * (main_h - overlay_h)")
                                .Set("shortest", "1")
                                .Set("format", "yuv420")
                        },
                    },
                },
            };

            return launcher;
        }
    }

    static (int width, int height) Scale(int width, int height, int maxpix)
    {
        var pixels = width * height;
        var outwidth = width;
        if (maxpix < pixels)
        {
            var scale = maxpix / (double) pixels;
            outwidth = (int) (width * Math.Sqrt(scale));
        }

        var outheight = (int) ((double) height / width * outwidth);

        outwidth += outwidth % 2;
        outheight += outheight % 2;
        return (outwidth, outheight);
    }


    static class QRGenerator
    {
        public static Image<Rgba32> Generate(
            string data,
            Image logo,
            int width,
            int height,
            float logoToQrRatio
        )
        {
            var qrCodeGenerator = new BarcodeWriterPixelData()
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions()
                {
                    Width = width,
                    Height = height,
                    ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.L,
                    Margin = 1,
                    NoPadding = true,
                },
            };

            var encodedData = qrCodeGenerator.Encode(data);
            var size = encodedData.Width * 2;
            var qrCode = render(encodedData, Color.White, Color.Black);

            var actualLogoSize = (int) (size * logoToQrRatio);
            using var logoclone = logo.Clone(ctx => ctx.Resize(new Size(actualLogoSize)));
            logo = logoclone;

            var center = (size - actualLogoSize) / 2;
            qrCode.Mutate(ctx => ctx
                .Resize(size, size, KnownResamplers.NearestNeighbor, false)
                .DrawImage(logo, new Point(center, center), opacity: 1)
            );

            return qrCode;


            static Image<Rgba32> render(BitMatrix matrix, Rgba32 background, Rgba32 foreground)
            {
                var image = new Image<Rgba32>(matrix.Width, matrix.Height, background);

                for (var i = 0; i < matrix.Height; i++)
                    for (var k = 0; k < matrix.Width; k++)
                        if (matrix[k, i])
                            image[k, i] = foreground;

                return image;
            }
        }
    }
}