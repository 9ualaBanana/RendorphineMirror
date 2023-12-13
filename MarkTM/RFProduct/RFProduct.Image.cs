using Node.Tasks.Exec.Output;
using Node.Tasks.Models;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record Image : RFProduct
    {
        internal static async ValueTask<RFProduct.Image> RecognizeAsync(string idea, ID_ id, AssetContainer container, CancellationToken cancellationToken)
            => new RFProduct.Image(idea, id, await QSPreviews.GenerateAsync<QSPreviews>(new string[] { idea }, cancellationToken), container);

        Image(string idea, ID_ id, QSPreviews previews, AssetContainer container)
            : base(idea, id, previews, container)
        {
        }


        [JsonConverter(typeof(QSPreviews.Converter))]
        new public record QSPreviews(
            [JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR) : RFProduct.QSPreviews
        {
            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR }.AsEnumerable().GetEnumerator();


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
