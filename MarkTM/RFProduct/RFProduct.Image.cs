using Node.Tasks.Exec.Output;
using Node.Tasks.Models;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record Image : RFProduct
    {
        public record Constructor : Constructor<Image>
        {
            internal override async Task<Image> CreateAsync(string idea, ID_ id, AssetContainer container, CancellationToken cancellationToken)
                => new(idea, id, await QSPreviews.GenerateAsync(idea, container, cancellationToken), container);
            public required QSPreviews.Generator QSPreviews { get; init; }
        }
        Image(string idea, ID_ id, QSPreviews previews, AssetContainer container)
            : base(new(idea), id, previews, container)
        {
        }


        new public record QSPreviews(
            [property: JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [property: JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR) : RFProduct.QSPreviews
        {
            public record Generator : Generator<Image.QSPreviews>
            {
            }

            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR }.AsEnumerable().GetEnumerator();
        }
    }
}
