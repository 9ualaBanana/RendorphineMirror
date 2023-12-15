using Node.Tasks.Exec.Output;
using Node.Tasks.Models;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record Image : RFProduct
    {
        Image(RFProduct core)
            : base(core)
        {
        }


        new public record QSPreviews(
            [JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR) : RFProduct.QSPreviews
        {
            public record Generator : Generator<Image.QSPreviews>
            {
            }

            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR }.AsEnumerable().GetEnumerator();
        }
    }
}
