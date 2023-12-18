using Node.Tasks.Exec.Output;
using Node.Tasks.Models;
using System.Diagnostics;
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
            : base(idea, id, previews, container)
        {
        }


        new public record QSPreviews(
            [JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR,
            [JsonProperty(nameof(QSPreviewOutput.Video))] FileWithFormat Video) : RFProduct.QSPreviews
        {
            public record Generator : Generator<Video.QSPreviews>
            {
                protected override async ValueTask<IReadOnlyList<string>> PrepareInputAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
                {
                    var videoScreenshot = await CaptureScreenshotAsync();
                    //File.Delete(videoScreenshot);
                    return new string[] { idea, videoScreenshot };


                    async Task<string> CaptureScreenshotAsync()
                    {
                        // Screenshot should be taken and when its QS preview is ready it shall be replaced with it.
                        var screenshot = System.IO.Path.Combine(container, $"{System.IO.Path.GetFileNameWithoutExtension(idea)}_ss.jpg");
                        var processInfo = new ProcessStartInfo(@"assets\ffmpeg.exe", $"-ss {(await DetermineVideoDurationAsync()).Divide(2):hh\\:mm\\:ss} -i \"{idea}\" -frames:v 1 -q:v 1 \"{screenshot}\"");
                        if (Process.Start(processInfo) is Process process)
                        { await process.WaitForExitAsync(cancellationToken); return screenshot; }
                        else throw new Exception("Failed to start ffmpeg process.");


                        async Task<TimeSpan> DetermineVideoDurationAsync()
                        {
                            var processInfo = new ProcessStartInfo(@"assets\ffprobe", $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 -sexagesimal \"{idea}\"")
                            {
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true
                            };
                            if (Process.Start(processInfo) is Process process)
                            {
                                await process.WaitForExitAsync(cancellationToken);
                                return TimeSpan.TryParse(process.StandardOutput.ReadToEnd(), out TimeSpan duration) ?
                                    duration : throw new Exception(process.StandardError.ReadToEnd());
                            }
                            else throw new Exception("Failed to start ffprobe process.");
                        }
                    }
                }
            }

            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR, Video }.AsEnumerable().GetEnumerator();
        }
    }
}
