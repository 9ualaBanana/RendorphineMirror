using Node.Tasks.Models;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    internal class Factory
    {
        internal static async Task<RFProduct> CreateAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
        {
            Directory.CreateDirectory(container);
            RFProduct product = FileFormatExtensions.FromFilename(idea) switch
            {
                FileFormat.Mov => await Video.RecognizeAsync(idea, container, cancellationToken, disposeTemps),
                FileFormat.Png or FileFormat.Jpeg => await Image.RecognizeAsync(idea, container, cancellationToken, disposeTemps),
                _ => throw new NotImplementedException($"{nameof(idea)} has an unsupported {nameof(FileFormat)}.")
            };

            // Create RFProducts out of nested product assets.
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
                if (assetName == product.ID
                    || assetName == System.IO.Path.GetFileName(idea)
                    || product.QSPreview.Any(preview => asset == preview))
                    return false;
                else return true;
            }
        }
    }
}
