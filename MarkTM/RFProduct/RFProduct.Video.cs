using Node.Tasks.Exec.Output;
using Node.Tasks.Models;
using System.Diagnostics;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record Video : RFProduct
    {
        new internal static async ValueTask<RFProduct.Video> RecognizeAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
        {
            // TODO: Extract base logic.
            var product = new RFProduct.Video(
                idea,
                await IDManager.GenerateIDAsync(System.IO.Path.GetFileName(container), cancellationToken),
                await GenerateQSPreviewsAsync(cancellationToken),
                container, disposeTemps);
            return product;


            // Implement Template Pattern so children should override core implementation
            async Task<QSPreviews> GenerateQSPreviewsAsync(CancellationToken cancellationToken, string? name = default)
            {
                name ??= System.IO.Path.GetFileNameWithoutExtension(idea);

                var videoScreenshot = await CaptureScreenshotAsync();
                var qsPreview = await QSPreviews.GenerateAsync<QSPreviews>(new string[] { idea, videoScreenshot }, cancellationToken);
                //File.Delete(videoScreenshot);
                return qsPreview;


                async Task<string> CaptureScreenshotAsync()
                {
                    // Screenshot should be taken and when its QS preview is ready it shall be replaced with it.
                    var screenshot = System.IO.Path.Combine(container, $"{name}_ss.jpg");
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

        Video(string idea, string id, QSPreviews previews, string container, bool disposeTemps)
            : base(idea, id, previews, container, disposeTemps)
        {
        }


        [JsonConverter(typeof(QSPreviews.Converter))]
        new public record QSPreviews(
            [JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR,
            [JsonProperty(nameof(QSPreviewOutput.Video))] FileWithFormat Video) : RFProduct.QSPreviews
        {
            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR, Video }.AsEnumerable().GetEnumerator();


            public class Converter : JsonConverter
            {
                public override bool CanConvert(Type objectType)
                    => typeof(QSPreviews).IsAssignableFrom(objectType);

                public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
                {
                    var json = JObject.Load(reader);
                    var target = new QSPreviews(
                        json[nameof(QSPreviewOutput.ImageFooter)]!.ToObject<FileWithFormat>(serializer)!,
                        json[nameof(QSPreviewOutput.ImageQr)]!.ToObject<FileWithFormat>(serializer)!,
                        json[nameof(QSPreviewOutput.Video)]!.ToObject<FileWithFormat>(serializer)!);
                    return target;
                }

                public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
                    => serializer.Serialize(writer, value);
            }
        }
    }
}
