using Node.Tasks;
using Node.Tasks.Models;
using Node.Tasks.Models.ExecInfo;
using System.Collections;
using System.Text;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

// Describe this complex pattern that requires generalization on an arbitrary amount of properties, also must support [de]serialization and stuff.
// This is needed to turn this information into a knowledge that can be used without going through the same process of re-learning each time I'm faced with that kind of a problem.
// That's why it's called "Pattern" - it is repeated. Why not automating this process by abstracting it into a well-defined knowledge.
public partial record RFProduct : AssetContainer
{
    [JsonProperty] public string Type
    {
        get => _type ?? this.GetType().Name;
        init => _type = value;
    }
    string? _type;
    public string ID { get; }
    public Idea_ Idea { get; }
    public QSPreviews QSPreview { get; }
    public ImmutableHashSet<RFProduct> SubProducts { get; private set; } = [];

    readonly static Encoding _encoding = Encoding.UTF8;

    /// <summary>
    /// Asynchronous constructor for <see cref="RFProduct"/> implementations.
    /// </summary>
    /// <typeparam name="TProduct">Concrete <see cref="RFProduct"/> implementation to construct.</typeparam>
    public abstract record Constructor<TIdea, TPreviews, TProduct>
        where TIdea : Idea_
        where TProduct : RFProduct
        where TPreviews : QSPreviews
    {
        public required Idea_.IRecognizer<TIdea> Recognizer { get; init; }
        public required QSPreviews.Generator<TPreviews> QSPreviews { get; init; }

        internal async Task<TProduct> CreateAsync(TIdea idea, string id, AssetContainer container, CancellationToken cancellationToken)
        {
            // FIX: QSPreviews and Idea paths don't change along with their container and it's fucked up.
            idea.Path = container.Store(idea.Path, @as: Idea_.FileName, StoreMode.Copy);
            var previews = await QSPreviews.GenerateAsync(await GetPreviewInputAsync(idea), container, cancellationToken);
            return Create(idea, id, previews, container);
        }
        // Represents virtual constructor method that merely delegates construction to concrete children constructors,
        // thus circumvents placing new() constraint on TProduct and creating it right here in the base class.
        internal abstract TProduct Create(TIdea idea, string id, TPreviews previews, AssetContainer container);
        protected virtual ValueTask<string> GetPreviewInputAsync(TIdea idea) => ValueTask.FromResult(idea.Path);
        internal virtual string[] SubProductsIdeas(TIdea idea) => [];
    }
    protected RFProduct(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
        : base(container)
    {
        Idea = idea;
        ID = id;
        QSPreview = previews;
        QSPreview.BindTo(this);
    }

    // TODO: Move the logic to JsonConverter ? this way seems easier tho.
    [JsonConstructor]
    RFProduct(JObject idea, string id, JObject qsPreview, JObject[] subproducts, string container, string type)
        : base(new(container))
    {
        Type = type;
        ID = id;
        try
        {
            QSPreview = type switch
            {
                nameof(_3D) or nameof(_3D.Renders) => qsPreview.ToObject<_3D.QSPreviews>()!,
                nameof(Video) => qsPreview.ToObject<Video.QSPreviews>()!,
                nameof(Image) => qsPreview.ToObject<Image.QSPreviews>()!,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"{nameof(Type)} of a serialized {nameof(RFProduct)} is unknown.")
            };
            ArgumentNullException.ThrowIfNull(QSPreview, $"Mismatch between {typeof(RFProduct)} {nameof(Type)} and its corresponding {nameof(QSPreviews)}.");
        }
        catch (Exception ex) { throw new JsonReaderException($"{nameof(QSPreviews)} deserialization failed.", ex); }
        try
        {
            Idea = type switch
            {
                nameof(_3D) => idea.ToObject<_3D.Idea_>()!,
                nameof(_3D.Renders) or nameof(Video) or nameof(Image) => idea.ToObject<Idea_>()!,
                _ => throw new InvalidOperationException($"{type} {nameof(RFProduct)} doesn't support type-specific {nameof(Idea)}.")
            };
            ArgumentNullException.ThrowIfNull(Idea, $"Mismatch between {typeof(RFProduct)} {nameof(Type)} and its corresponding {nameof(Idea)}.");
        }
        catch (Exception ex) { throw new JsonReaderException($"{nameof(Idea)} deserialization failed.", ex); }
        SubProducts = subproducts.Select(_ => _.ToObject<RFProduct>()).ToImmutableHashSet()!;
    }


    // Idea_.Generator might be necessary as soon as asynchronous operations needs to be performed for obtaining it. For now virtual properties suffice.
    public record Idea_
    {
        internal const string FileName = "idea";
        /// <remarks><see langword="internal set"/> for now to support the ad-hoc binding inside <see cref="RFProduct.Store(string, string?, StoreMode)"/> methods.</remarks>
        public string Path { get; internal set; }

        [JsonConstructor]
        internal Idea_(string path)
        {
            Path = path;
        }

        public interface IRecognizer<TIdea>
        {
            TIdea? TryRecognize(string idea);
        }
    }


    [JsonObject]
    public abstract record QSPreviews : IEnumerable<FileWithFormat>
    {
        public record Generator<QSPreviews_>
            where QSPreviews_ : QSPreviews
        {
            public required ITaskExecutor TaskExecutor { get; init; }
            public required ILogger<Generator<QSPreviews_>> Logger { get; init; }

            internal async Task<QSPreviews_> GenerateAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
            {
                if (!File.Exists(idea))
                    throw new FileNotFoundException($"Idea for {nameof(QSPreviews)} wasn't found or doesn't refer to a file.", idea);

                var qsOutput = await TaskExecutor.ExecuteQS(
                    await PrepareInputAsync(idea, container, cancellationToken),
                    new QSPreviewInfo(Guid.NewGuid().ToString()) { AlwaysGenerateQRPreview = true },
                    cancellationToken);
                return JObject.FromObject(qsOutput).ToObject<QSPreviews_>() ??
                    throw new InvalidCastException($"{nameof(QSPreviews)} generation endpoint returned data in a wrong format.");
            }

            protected virtual ValueTask<IReadOnlyList<string>> PrepareInputAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
                => ValueTask.FromResult(new string[] { idea } as IReadOnlyList<string>);
        }

        public void BindTo(RFProduct product)
        {
            foreach (var preview in this)
                preview.MoveTo(product, name: $"qs_{System.IO.Path.GetFileName(preview)}");
        }

        public abstract IEnumerator<FileWithFormat> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        { throw new NotImplementedException(); }
    }
}
