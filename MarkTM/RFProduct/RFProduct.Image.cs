using Node.Tasks.Exec.Output;
using Node.Tasks.Models;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record Image : RFProduct
    {
        internal static async ValueTask<RFProduct.Image> RecognizeAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
            => new(await RFProduct.RecognizeAsync(idea, await QSPreviews.GenerateAsync<QSPreviews>(new string[] { idea }, cancellationToken), container, cancellationToken));

        Image(RFProduct core)
            : base(core)
        {
        }


        new public record QSPreviews(
            [JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR) : RFProduct.QSPreviews
        {
            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR }.AsEnumerable().GetEnumerator();
        }
    }
}
