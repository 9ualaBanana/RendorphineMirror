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
        public record Constructor : Constructor<Idea_, QSPreviews, Video>
        {
            internal override Video Create(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
                => new(idea, id, previews, container);
        }
        Video(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
            : base(idea, id, previews, container)
        {
        }


        new public record Idea_
            : RFProduct.Idea_
        {
            Idea_(string path)
                : base(path)
            {
            }
            public record Recognizer : IRecognizer<Idea_>
            {
                public Idea_? TryRecognize(string idea)
                    => File.Exists(idea) && FileFormatExtensions.FromFilename(idea) is FileFormat.Mov ? new(idea) : null;
            }
        }

        new public record QSPreviews(
            [property: JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [property: JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR,
            [property: JsonProperty(nameof(QSPreviewOutput.Video))] FileWithFormat Video) : RFProduct.QSPreviews
        {
            public record Generator : Generator<QSPreviews>
            {
                public required IPluginList PluginList { get; init; }

                protected override async ValueTask<IReadOnlyList<string>> PrepareInputAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
                {
                    var videoScreenshot = await CaptureScreenshotAsync();
                    return new string[] { idea, videoScreenshot };


                    async Task<string> CaptureScreenshotAsync()
                    {
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
