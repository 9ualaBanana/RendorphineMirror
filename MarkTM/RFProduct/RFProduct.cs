using Node.Common.Models;
using Node.Tasks;
using Node.Tasks.Models;
using Node.Tasks.Models.ExecInfo;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct : AssetContainer
{
    [JsonProperty] public string Type
    {
        get => _type ?? this.GetType().Name;
        init => _type = value;
    }
    string? _type;
    public ID_ ID { get; }
    // TODO: Bind Data to RFProduct.
    /// <remarks><see cref="Idea"/> is implemented akin to <see cref="QSPreview"/>.</remarks>
    public Idea_ Idea { get; }
    public QSPreviews QSPreview { get; }
    public ImmutableHashSet<RFProduct> SubProducts { get; private set; } = [];

    readonly static Encoding _encoding = Encoding.UTF8;

    /// <summary>
    /// Asynchronous constructor for <see cref="RFProduct"/> implementations.
    /// </summary>
    /// <typeparam name="TProduct">Concrete <see cref="RFProduct"/> implementation to construct.</typeparam>
    public abstract record Constructor<TProduct> where TProduct : RFProduct
    {
        // Move QSPreviews generator here to ABC ?
        internal async Task<TProduct> CreateAsync_(string idea, ID_ id, AssetContainer container, Factory factory, CancellationToken cancellationToken)
        {
            var product = await CreateAsync(idea, id, container, cancellationToken);
            product.SubProducts = (await CreateSubProductsAsync(product, factory, cancellationToken)).ToImmutableHashSet();
            return product;
        }
        internal abstract Task<TProduct> CreateAsync(string idea, ID_ id, AssetContainer container, CancellationToken cancellationToken);
        protected virtual Task<RFProduct[]> CreateSubProductsAsync(TProduct product, Factory factory, CancellationToken cancellationToken)
            => Task.FromResult<RFProduct[]>([]);
    
    }
    protected RFProduct(Idea_ idea, ID_ id, QSPreviews previews, AssetContainer container)
        : base(container)
    {
        Idea = idea;
        ID = id;
        QSPreview = previews;
        QSPreview.BindTo(this);
    }

    // TODO: Move the logic to JsonConverter ? this way seems easier tho.
    [JsonConstructor]
    RFProduct(JObject idea, ID_ id, JObject qsPreview, JObject[] subproducts, string container, string type)
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
                nameof(Video) or nameof(Image) => idea.ToObject<Idea_>()!,
                _ => throw new InvalidOperationException($"{type} {nameof(RFProduct)} doesn't support type-specific {nameof(Idea)}.")
            };
            ArgumentNullException.ThrowIfNull(Idea, $"Mismatch between {typeof(RFProduct)} {nameof(Type)} and its corresponding {nameof(Idea)}.");
        }
        catch (Exception ex) { throw new JsonReaderException($"{nameof(Idea)} deserialization failed.", ex); }
        SubProducts = subproducts.Select(_ => _.ToObject<RFProduct>()).ToImmutableHashSet()!;
    }

    new public string Store(AssetContainer container, string? @as = default, StoreMode mode = StoreMode.Move)
    {
        var storedFile = base.Store(container, @as, mode);
        // TODO: Fix this.
        if (System.IO.Path.GetFileNameWithoutExtension(@as) == Idea_.FileName)
            Idea.Path = storedFile;

        return storedFile;
    }

    new public string Store(string file, string? @as = default, StoreMode mode = StoreMode.Move)
    {
        var storedFile = base.Store(file, @as, mode);
        // TODO: Fix this.
        if (System.IO.Path.GetFileNameWithoutExtension(@as) == Idea_.FileName)
            Idea.Path = storedFile;

        return storedFile;
    }


    // Idea_.Generator might be necessary as soon as asynchronous operations needs to be performed for obtaining it. For now virtual properties suffice.
    public record Idea_
    {
        internal const string FileName = "idea";
        /// <remarks><see langword="internal"/> for now to support the ad-hoc binding inside <see cref="RFProduct.Store(string, string?, StoreMode)"/> methods.</remarks>
        public string Path { get; internal set; }

        [JsonConstructor]
        internal Idea_(string path)
        {
            Path = path;
        }
    }

    public record ID_
    {
        public string Value { get; }
        [JsonProperty] internal File_ File { get; }

        ID_(string id, string container)
        {
            Value = id;
            File = new(id, container);
        }

        [JsonConstructor]
        ID_(string value, File_ file)
        {
            Value = value;
            File = file;
        }

        public static implicit operator string(ID_ id) => id.Value;

        public class Generator
        {
            public required INodeSettings NodeSettings { get; init; }

            internal async Task<ID_> AssignedTo(AssetContainer product, CancellationToken cancellationToken)
                => ID_.File_.FindInside(product) ?? await GenerateAsync(product, cancellationToken);

            public async Task<ID_> GenerateAsync(AssetContainer container, CancellationToken cancellationToken)
            {
                using var productNameStream = new MemoryStream(_encoding.GetBytes(System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(container))));
                var id = Convert.ToBase64String(await HMACSHA512.HashDataAsync(_encoding.GetBytes(NodeSettings.AuthInfo.ThrowIfNull("Not authenticated").Guid), productNameStream, cancellationToken))
                    .Replace('/', '-')
                    .Replace('+', '_');
                return new ID_(id, container);
            }
        }


        internal record File_
        {
            [JsonIgnore] public string Name => _file.Name;
            readonly FileInfo _file;
            [JsonProperty] readonly string path;
            internal const string Extension = ".rfpid";

            internal static ID_? FindInside(AssetContainer container)
                => container.EnumerateEntries(EntryType.NonContainers)
                .SingleOrDefault(_ => System.IO.Path.GetExtension(_) == ID_.File_.Extension) is string file ?

                new(System.IO.Path.GetFileNameWithoutExtension(file), container) : null;

            internal File_(string id, string container)
            {
                _file = new FileInfo(path = System.IO.Path.Combine(container, $"{id}{Extension}"));
                if (!_file.Exists) { using var _ = _file.Create(); }
                _file.Attributes |= FileAttributes.Hidden;
            }

            [JsonConstructor]
            File_(string path)
            { _file = new(this.path = path); }
        }
    }

    [JsonObject]
    public abstract record QSPreviews : IEnumerable<FileWithFormat>
    {
        public record Generator<QSPreviews_>
            where QSPreviews_ : QSPreviews
        {
            public required INodeSettings NodeSettings { get; init; }
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
