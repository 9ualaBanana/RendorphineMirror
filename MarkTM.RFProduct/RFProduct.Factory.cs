using Node.Common.Models;
using System.Diagnostics.CodeAnalysis;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public class Factory
    {
        public required _3D.Constructor _3D { get; init; }
        public required Video.Constructor Video { get; init; }
        public required Image.Constructor Image { get; init; }
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

            try
            {
                List<Exception> recognitionExceptions = [];
                Idea_? idea_ = null;
                try { idea_ = _3D.Recognizer.Recognize(idea); }
                catch (Exception ex) { recognitionExceptions.Add(ex); }
                try { idea_ = Video.Recognizer.Recognize(idea); }
                catch (Exception ex) { recognitionExceptions.Add(ex); }
                try { idea_ = Image.Recognizer.Recognize(idea); }
                catch (Exception ex) { recognitionExceptions.Add(ex); }
                
                if (idea_ is not null)
                    return await CreateAsync(idea_, container, cancellationToken);
                else throw new AggregateException(recognitionExceptions);
            }
            catch
            {
                foreach (var qsPreview in container.EnumerateEntries(EntryType.NonContainers).Where(_ => _.StartsWith("qs_")))
                    File.Delete(qsPreview);
                throw;
            }


        }
        internal async Task<RFProduct> CreateAsync(Idea_ idea, AssetContainer container, CancellationToken cancellationToken)
        {
            RFProduct product = idea switch
            {
                _3D.Idea_ idea_ => (await _3D.CreateAsync(idea_, container, cancellationToken)) with { SubProducts = await CreateSubProductsAsync(idea_, _3D, cancellationToken) },
                Video.Idea_ idea_ => await Video.CreateAsync(idea_, container, cancellationToken) with { SubProducts = await CreateSubProductsAsync(idea_, Video, cancellationToken) },
                Image.Idea_ idea_ => await Image.CreateAsync(idea_, container, cancellationToken) with { SubProducts = await CreateSubProductsAsync(idea_, Image, cancellationToken) },
                _ => throw new NotImplementedException()
            };

            Storage.RFProducts.Add(product);

            return product;


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
