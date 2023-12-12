using Node.Tasks.Exec.Output;
using Node.Tasks.Models;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record Image : RFProduct
    {
        new internal static async ValueTask<RFProduct.Image> RecognizeAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
        {
            var product = new RFProduct.Image(
                idea,
                await IDManager.GenerateIDAsync(System.IO.Path.GetFileName(container), cancellationToken),
                await QSPreviews.GenerateAsync<QSPreviews>(new string[] { idea }, cancellationToken),
                container, disposeTemps);
            return product;
        }

        Image(string idea, string id, QSPreviews previews, string container, bool disposeTemps)
            : base(idea, id, previews, container, disposeTemps)
        {
        }


        [JsonConverter(typeof(QSPreviews.Converter))]
        new public record QSPreviews(
            [JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR) : RFProduct.QSPreviews
        {
            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR }.AsEnumerable().GetEnumerator();


            // Required for regular properties mapping because NewtonsoftJson tries to find JSON array fro mappeing to classes implementing IEnumerable.
            public class Converter : JsonConverter
            {
                public override bool CanConvert(Type objectType)
                    => typeof(QSPreviews).IsAssignableFrom(objectType);

                public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
                {
                    var json = JObject.Load(reader);
                    var target = new QSPreviews(
                        json[nameof(QSPreviewOutput.ImageFooter)]!.ToObject<FileWithFormat>(serializer)!,
                        json[nameof(QSPreviewOutput.ImageQr)]!.ToObject<FileWithFormat>(serializer)!);
                    return target;
                }

                public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
                    => serializer.Serialize(writer, value);
            }
        }
    }
}
