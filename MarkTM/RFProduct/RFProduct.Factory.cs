using Node.Tasks.Models;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    internal class Factory
    {
        internal static async Task<RFProduct> CreateAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
            => await CreateAsync(idea, AssetContainer.Create(container, disposeTemps), cancellationToken);
        static async Task<RFProduct> CreateAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
        {
            var id = await ID_.AssignedTo(container, cancellationToken);
            RFProduct product = FileFormatExtensions.FromFilename(idea) switch
            {
                FileFormat.Mov => await Video.RecognizeAsync(idea, id, container, cancellationToken),
                FileFormat.Png or FileFormat.Jpeg => await Image.RecognizeAsync(idea, id, container, cancellationToken),
                _ => throw new NotImplementedException($"{nameof(RFProduct.Idea)} has an unsupported {nameof(FileFormat)}.")
            };

            foreach (var asset in product.EnumerateFiles().Where(IsValidProduct))
                await RFProduct.Factory.CreateAsync(
                    asset,
                    System.IO.Path.Combine(container, System.IO.Path.GetFileNameWithoutExtension(asset)),
                    cancellationToken);
            return product;


            // TODO: Implement unique universal way of denoting assets of already existing RFProduct that should not be turned into nested RFProducts.
            bool IsValidProduct(string asset)
            {
                var assetName = System.IO.Path.GetFileName(asset);
                if (assetName == product.ID.File.Name
                    || Idea_.Exists(asset)
                    || product.QSPreview.Any(preview => asset == preview))
                    return false;
                else return true;
            }
        }
    }
}
