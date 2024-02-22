using Node.Common.Models;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public class Factory
    {
        public required Video.Constructor Video { get; init; }
        public required Image.Constructor Image { get; init; }
        public required _3D.Constructor _3D { get; init; }
        public required IRFProductStorage Storage { get; init; }
        public required INodeSettings NodeSettings { get; init; }


        public async Task<RFProduct> CreateAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
            => await CreateAsync(idea, AssetContainer.Create(container, disposeTemps), cancellationToken);
        /// <summary>
        /// Creates a new <see cref="RFProduct"/> if <paramref name="container"/> representing the resulting product is not identified by the <see cref="IRFProductStorage"/>;
        /// otherwise, <see cref="RFProduct"/> object is deserialized from the <see cref="IRFProductStorage"/>.
        /// </summary>
        public async Task<RFProduct> CreateAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
        {
            if (Archive_.IsArchive(idea))
                idea = Archive_.Unpack(idea);

            var id = await GenerateIDAsync(cancellationToken);
            if (Storage.RFProducts.TryGetValue(id, out var product))
                return product;
            else
            {
                try
                {
                    if (_3D.Recognizer.TryRecognize(idea) is _3D.Idea_ _3dIdea)
                        product = await CreateAsync<_3D.Idea_, _3D>(_3dIdea, id, container, cancellationToken);
                    else if (Video.Recognizer.TryRecognize(idea) is Video.Idea_ videoIdea)
                        product = await CreateAsync<Video.Idea_, Video>(videoIdea, id, container, cancellationToken);
                    else if (Image.Recognizer.TryRecognize(idea) is Image.Idea_ imageIdea)
                        product = await CreateAsync<Image.Idea_, Image>(imageIdea, id, container, cancellationToken);
                    else throw new NotImplementedException();

                    return product;
                }
                catch { container?.Delete(); throw; }
            }


            static async Task<string> GenerateIDAsync(CancellationToken cancellationToken)
            {
                string userFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "renderfin", "user");
                if (!File.Exists(userFile)) File.Create(userFile).Dispose();
                var userId = (await File.ReadAllLinesAsync(userFile, cancellationToken)).SingleOrDefault();
                if (userId is null)
                {
                    userId = Guid.NewGuid().ToString();
                    File.WriteAllText(userFile, userId);
                }

                using var productNameStream = new MemoryStream(_encoding.GetBytes(Guid.NewGuid().ToString()));
                return Convert.ToBase64String(await HMACSHA512.HashDataAsync(_encoding.GetBytes(userId), productNameStream, cancellationToken))
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
                _3D.Idea_ idea_ => (await _3D.CreateAsync(idea_, id, container, cancellationToken)) with { SubProducts = await CreateSubProductsAsync(idea_, _3D, cancellationToken) },
                Video.Idea_ idea_ => await Video.CreateAsync(idea_, id, container, cancellationToken) with { SubProducts = await CreateSubProductsAsync(idea_, Video, cancellationToken) },
                Image.Idea_ idea_ => await Image.CreateAsync(idea_, id, container, cancellationToken) with { SubProducts = await CreateSubProductsAsync(idea_, Image, cancellationToken) },
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
                    // Pass proper container name so it doesn't complain about already existing file entry with such name.
                    foreach (var subproduct in constructor.SubProductsIdeas(idea).Select(_ => CreateAsync(_, System.IO.Path.ChangeExtension(_, null), cancellationToken)))
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
