using Node.Tasks.Models;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public class Factory
    {
        public required ID_.Generator ID { get; init; }
        public required Video.QSPreviews.Generator VideoQSPreviews { get; init; }
        public required Image.QSPreviews.Generator ImageQSPreviews { get; init; }
        public required IRFProductStorage Storage { get; init; }

        public async Task<RFProduct> CreateAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
            => await CreateAsync(idea, AssetContainer.Create(container, disposeTemps), cancellationToken);
        async Task<RFProduct> CreateAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
        {
            // Only QSPreviews differ at that point so RFProduct should be generic only on one QSPreviews type argument.
            ID_ id = await ID.AssignedTo(container, cancellationToken);
            if (Storage.RFProducts.TryGetValue(id, out var product))
                return product;
            else
            {
                QSPreviews previews = FileFormatExtensions.FromFilename(idea) switch
                {
                    FileFormat.Mov => await VideoQSPreviews.GenerateAsync(idea, container, cancellationToken),
                    FileFormat.Png or FileFormat.Jpeg => await ImageQSPreviews.GenerateAsync(idea, container, cancellationToken),
                    _ => throw new NotImplementedException($"{nameof(idea)} has an unsupported {nameof(FileFormat)}.")
                };
                product = new(id, previews, container);
                product.Store(idea, @as: System.IO.Path.ChangeExtension(Idea.FileName, System.IO.Path.GetExtension(idea)), StoreMode.Copy);

                Storage.RFProducts.Add(product);

                foreach (var asset in product.EnumerateFiles().Where(IsValidProduct))
                    await CreateAsync(asset, System.IO.Path.Combine(container, System.IO.Path.GetFileNameWithoutExtension(asset)), cancellationToken);

                return product;
            }


            // TODO: Implement unique universal way of denoting assets of already existing RFProduct that should not be turned into nested RFProducts.
            bool IsValidProduct(string asset)
            {
                var assetName = System.IO.Path.GetFileName(asset);
                if (assetName == product.ID.File.Name
                    || Idea.Exists(asset)
                    || product.QSPreview.Any(preview => asset == preview))
                    return false;
                else return true;
            }
        }
    }
}
