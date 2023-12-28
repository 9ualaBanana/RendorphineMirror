using Node.Common;
using Node.Plugins.Models;
using Node.Tasks.Exec.FFmpeg;
using Node.Tasks.Exec.FFmpeg.Codecs;
using Node.Tasks.Exec.Output;
using Node.Tasks.Models;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record Video : RFProduct
    {
        public record Constructor : Constructor<Video>
        {
            internal override async Task<Video> CreateAsync(string idea, ID_ id, AssetContainer container, CancellationToken cancellationToken)
                => new(idea, id, await QSPreviews.GenerateAsync(idea, container, cancellationToken), container);
            public required QSPreviews.Generator QSPreviews { get; init; }
        }
        Video(string idea, ID_ id, QSPreviews previews, AssetContainer container)
            : base(new(idea), id, previews, container)
        {
        }


        new public record QSPreviews(
            [property: JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [property: JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR,
            [property: JsonProperty(nameof(QSPreviewOutput.Video))] FileWithFormat Video) : RFProduct.QSPreviews
        {
            public record Generator : Generator<Video.QSPreviews>
            {
                public required IPluginList PluginList { get; init; }

                protected override async ValueTask<IReadOnlyList<string>> PrepareInputAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
                {
                    var videoScreenshot = await CaptureScreenshotAsync();
                    //File.Delete(videoScreenshot);
                    return new string[] { idea, videoScreenshot };


                    async Task<string> CaptureScreenshotAsync()
                    {
                        // Screenshot should be taken and when its QS preview is ready it shall be replaced with it.
                        var screenshot = System.IO.Path.Combine(container, $"{System.IO.Path.GetFileNameWithoutExtension(idea)}_ss.jpg");

                        var launcher = new FFmpegLauncher(PluginList.GetPlugin(PluginType.FFmpeg).Path)
                        {
                            Input = { idea },
                            Outputs =
                            {
                                new FFmpegLauncherOutput()
                                {
                                    Output = screenshot,
                                    Codec = new JpegFFmpegCodec(),
                                    Args = { "-ss", ((await FFProbe.Get(idea, Logger)).Duration / 2).ToStringInvariant() },
                                },
                            },
                            ILogger = Logger,
                        };
                        await launcher.Execute();

                        return screenshot;
                    }
                }
            }

            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR, Video }.AsEnumerable().GetEnumerator();
        }
    }
}
