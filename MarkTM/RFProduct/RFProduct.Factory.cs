using Node.Common.Models;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public class Factory
    {
        public required Video.Constructor Video { get; init; }
        public required Image.Constructor Image { get; init; }
        public required _3D.Constructor _3D { get; init; }
        public required _3D.Renders.Constructor Renders { get; init; }
        public required IRFProductStorage Storage { get; init; }
        public required INodeSettings NodeSettings { get; init; }


        public async Task<RFProduct> CreateAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
            => await CreateAsync(idea, AssetContainer.Create(container, disposeTemps), cancellationToken);
        /// <summary>
        /// Creates a new <see cref="RFProduct"/> if <paramref name="container"/> representing the resulting product is not identified by the <see cref="IRFProductStorage"/>;
        /// otherwise, <see cref="RFProduct"/> object is deserialized from the <see cref="IRFProductStorage"/>.
        /// </summary>
        async Task<RFProduct> CreateAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
        {
            var id = await ID(container, cancellationToken);
            if (Storage.RFProducts.TryGetValue(id, out var product))
                return product;
            else
            {
                if (_3D.Recognizer.TryRecognize(idea) is _3D.Idea_ _3dIdea)
                    product = await CreateAsync<_3D.Idea_, _3D>(_3dIdea, id, container, cancellationToken);
                else if (Renders.Recognizer.TryRecognize(idea) is _3D.Renders.Idea_ _3dRendersIdea)
                    product = await CreateAsync<_3D.Renders.Idea_, _3D.Renders>(_3dRendersIdea, id, container, cancellationToken);
                else if (Video.Recognizer.TryRecognize(idea) is Video.Idea_ videoIdea)
                    product = await CreateAsync<Video.Idea_, Video>(videoIdea, id, container, cancellationToken);
                else if (Image.Recognizer.TryRecognize(idea) is Image.Idea_ imageIdea)
                    product = await CreateAsync<Image.Idea_, Image>(imageIdea, id, container, cancellationToken);
                else throw new NotImplementedException();

                return product;
            }


            async Task<string> ID(AssetContainer container, CancellationToken cancellationToken)
            {
                using var productNameStream = new MemoryStream(_encoding.GetBytes(System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(container))));
                return Convert.ToBase64String(await HMACSHA512.HashDataAsync(_encoding.GetBytes(NodeSettings.AuthInfo.ThrowIfNull("Not authenticated").Guid), productNameStream, cancellationToken))
                    .Replace('/', '-')
                    .Replace('+', '_');
            }
        }
        internal async Task<TProduct> CreateAsync<TIdea, TProduct>(TIdea idea, string id, AssetContainer container, CancellationToken cancellationToken)
            where TIdea : Idea_
            where TProduct : RFProduct
        {
            RFProduct product = idea switch
            {
                _3D.Idea_ idea_ => (await _3D.CreateAsync_(idea_, id, container, cancellationToken)) with { SubProducts = await CreateSubProductsAsync(idea_, _3D, cancellationToken) },
                _3D.Renders.Idea_ idea_ => await Renders.CreateAsync_(idea_, id, container, cancellationToken) with { SubProducts = await CreateSubProductsAsync(idea_, Renders, cancellationToken) },
                Video.Idea_ idea_ => await Video.CreateAsync_(idea_, id, container, cancellationToken) with { SubProducts = await CreateSubProductsAsync(idea_, Video, cancellationToken) },
                Image.Idea_ idea_ => await Image.CreateAsync_(idea_, id, container, cancellationToken) with { SubProducts = await CreateSubProductsAsync(idea_, Image, cancellationToken) },
                _ => throw new NotImplementedException()
            };

            Storage.RFProducts.Add(product);

            return product as TProduct ?? throw new TypeInitializationException(typeof(TProduct).FullName, default);


            async Task<ImmutableHashSet<RFProduct>> CreateSubProductsAsync<TIdea, TPreviews, TProduct>(
                TIdea idea,
                Constructor<TIdea, TPreviews, TProduct> constructor,
                CancellationToken cancellationToken)
                    where TIdea : Idea_
                    where TProduct : RFProduct
                    where TPreviews : QSPreviews
            {
                return [.. (await CreateSubProductsAsyncCore().ToHashSetAsync(cancellationToken))];


                async IAsyncEnumerable<RFProduct> CreateSubProductsAsyncCore()
                {
                    foreach (var subproduct in constructor.SubProductsIdeas(idea).Select(_ => CreateAsync(_, _, cancellationToken)))
                        yield return await subproduct;
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
