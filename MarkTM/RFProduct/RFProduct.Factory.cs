using Node.Tasks.Models;
using NodeCommon.Tasks;
using NodeCommon.Tasks.Watching;
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
        public required _3D.Constructor _3D { get; init; }
        public required _3D.Renders.Constructor Renders { get; init; }
        public required IRFProductStorage Storage { get; init; }
        OneClickRunnerInfo OC { get; init; }
            = new OneClickRunnerInfo(
                new OneClickWatchingTaskInputInfo(
                    @"C:\Users\User\Documents\oc\input",
                    @"C:\Users\User\Documents\oc\output",
                    @"C:\Users\User\Documents\oc\log","","","","")
                );


        public async Task<RFProduct> CreateAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
            => await CreateAsync(idea, AssetContainer.Create(container, disposeTemps), cancellationToken);
        /// <summary>
        /// Creates a new <see cref="RFProduct"/> if <paramref name="container"/> representing the resulting product is not identified by the <see cref="IRFProductStorage"/>;
        /// otherwise, <see cref="RFProduct"/> object is deserialized from the <see cref="IRFProductStorage"/>.
        /// </summary>
        async Task<RFProduct> CreateAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
        {
            ID_ id = await ID.AssignedTo(container, cancellationToken);
            if (Storage.RFProducts.TryGetValue(id, out var product))
                return product;
            else
            {
                AssetContainer? ideaContainer = null;
                try
                {
                    if (AssetContainer.Exists(idea))
                    {
                        ideaContainer = new AssetContainer(idea);
                        if (RFProduct._3D.Idea_.Inside(ideaContainer) is _3D.Idea_ data)
                            product = await CreateAsync<_3D>(idea, id, container, cancellationToken);
                        else throw new NotImplementedException();
                    }
                    else if (File.Exists(idea))
                        product = FileFormatExtensions.FromFilename(idea) switch
                        {
                            FileFormat.Mov => await CreateAsync<Video>(idea, id, container, cancellationToken),
                            FileFormat.Png or FileFormat.Jpeg => await CreateAsync<Image>(idea, id, container, cancellationToken),
                            _ => throw new NotImplementedException($"{nameof(idea)} has an unsupported {nameof(FileFormat)}.")  // Change FileFormat to something more generic.
                        };
                    else throw new FileNotFoundException($"{nameof(idea)} for {nameof(RFProduct._3D)} {nameof(RFProduct)} was not found.", idea);
                }
                finally { ideaContainer?.Dispose(); }

                return product;
            }
        }
        internal async Task<TProduct> CreateAsync<TProduct>(string idea, ID_ id, AssetContainer container, CancellationToken cancellationToken)
            where TProduct : RFProduct
        {
            AssetContainer? ideaContainer = null;
            try
            {
                RFProduct product = typeof(TProduct) switch
                {
                    Type type when type == typeof(_3D) => await _3D.CreateAsync_(idea, id, container, this, cancellationToken),
                    Type type when type == typeof(_3D.Renders) => await Renders.CreateAsync_(idea, id, container, this, cancellationToken),
                    Type type when type == typeof(Video) => await Video.CreateAsync_(idea, id, container, this, cancellationToken),
                    Type type when type == typeof(Image) => await Image.CreateAsync_(idea, id, container, this, cancellationToken),
                    _ => throw new NotImplementedException()
                };

                Storage.RFProducts.Add(product);

                return product as TProduct ?? throw new TypeInitializationException(typeof(TProduct).FullName, default);
            }
            finally { ideaContainer?.Dispose(); }
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
