using Node.Tasks.Models;
using System.Diagnostics.CodeAnalysis;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public class Factory
    {
        public required ID_.Generator ID { get; init; }
        public required Video.Constructor Video { get; init; }
        public required Image.Constructor Image { get; init; }
        public required IRFProductStorage Storage { get; init; }

        public async Task<RFProduct> CreateAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
            => await CreateAsync(idea, AssetContainer.Create(container, disposeTemps), cancellationToken);
        async Task<RFProduct> CreateAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
        {
            ID_ id = await ID.AssignedTo(container, cancellationToken);
            if (Storage.RFProducts.TryGetValue(id, out var product))
                return product;
            else
            {
                product = FileFormatExtensions.FromFilename(idea) switch
                {
                    FileFormat.Mov => await Video.CreateAsync(idea, id, container, cancellationToken),
                    FileFormat.Png or FileFormat.Jpeg => await Image.CreateAsync(idea, id, container, cancellationToken),
                    _ => throw new NotImplementedException($"{nameof(idea)} has an unsupported {nameof(FileFormat)}.")
                };
                product.Store(idea, @as: System.IO.Path.ChangeExtension(Idea.FileName, System.IO.Path.GetExtension(idea)), StoreMode.Copy);

                var subProducts = new HashSet<RFProduct>(IDEqualityComparer._);
                foreach (var asset in product.EnumerateFiles().Where(IsValidProduct))
                    subProducts.Add(
                        await CreateAsync(asset, System.IO.Path.Combine(container, System.IO.Path.GetFileNameWithoutExtension(asset)), cancellationToken)
                        );
                product.SubProducts = subProducts.ToImmutableHashSet();

                Storage.RFProducts.Add(product);

                return product;


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
    internal class IDEqualityComparer : IEqualityComparer<RFProduct>
    {
        internal static IDEqualityComparer _ = new();
        public bool Equals(RFProduct? this_, RFProduct? that_)
            => this_?.ID == that_?.ID;
        public int GetHashCode([DisallowNull] RFProduct obj)
            => obj.ID.GetHashCode();
    }
}
