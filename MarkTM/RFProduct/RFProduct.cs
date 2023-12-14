using Node.Common;
using Node.Tasks;
using Node.Tasks.Models;
using Node.Tasks.Models.ExecInfo;
using NodeToUI;
using System.Collections;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public interface IRFProductRecognizer
{
    static abstract ValueTask<RFProduct> RecognizeAsync(string idea, RFProduct.ID_ id, AssetContainer container, CancellationToken cancellationToken);
}

public partial record RFProduct : AssetContainer
{
    public string Idea { get; }
    public ID_ ID { get; }
    public QSPreviews.Bound QSPreview { get; }

    readonly static Encoding _encoding = Encoding.UTF8;

    public static async ValueTask<RFProduct?> TryRecognizeAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
    {
        try { return await RecognizeAsync(idea, container, cancellationToken, disposeTemps); }
        catch { return null; }
    }

    // TODO: Check if RFProduct already exists and return it if so.
    public static async ValueTask<RFProduct> RecognizeAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
        => await RFProduct.Factory.CreateAsync(idea, container, cancellationToken, disposeTemps);

    /// <summary>
    /// <see langword="async"/> constructor that disables <see cref="RFProduct"/> ability to be abstract as an instance of the class must be obtained as the <see langword="base"/> reference for children and their constructors.
    /// </summary>
    /// <returns><see langword="base"/> reference for children constructors.</returns>
    protected static async ValueTask<RFProduct> RecognizeAsync(string idea, QSPreviews previews, AssetContainer container, CancellationToken cancellationToken)
        => new(idea, await ID_.AssignedTo(container, cancellationToken), previews, container);

    protected RFProduct(string idea, ID_ id, QSPreviews previews, AssetContainer container)
        : base(container)
    {
        Idea = idea;
        ID = id;
        QSPreview = QSPreviews.Bound.To(this, previews);

        // Not sure if it supports archive containers.
        Store(idea, @as: System.IO.Path.ChangeExtension(Idea_.FileName, System.IO.Path.GetExtension(idea)), StoreMode.Copy);
    }


    static class Idea_
    {
        internal static bool Exists(string idea)
            => File.Exists(idea) && System.IO.Path.GetFileNameWithoutExtension(idea) == FileName;

        internal const string FileName = "idea";
    }
    
    public record ID_
    {
        readonly string _value;
        internal File_ File { get; }

        ID_(string id, string container)
        {
            _value = id;
            File = new(id, container);
        }

        internal static async Task<ID_> AssignedTo(AssetContainer product, CancellationToken cancellationToken)
            => ID_.File_.FindInside(product) ?? await ID_.File_.GenerateAsync(product, cancellationToken);

        public static implicit operator string(ID_ id) => id._value;


        internal record File_
        {
            internal string Name => _file.Name;
            readonly FileInfo _file;
            internal const string Extension = ".rfpid";

            internal static ID_? FindInside(AssetContainer container)
                => container.EnumerateFiles(FilesToEnumerate.NonContainers).SingleOrDefault(_ => System.IO.Path.GetExtension(_) == File_.Extension) is string file ?
                new(System.IO.Path.GetFileNameWithoutExtension(file), container) : null;

            internal static async Task<ID_> GenerateAsync(AssetContainer container, CancellationToken cancellationToken)
            {
                using var productNameStream = new MemoryStream(_encoding.GetBytes(System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(container))));
                var id = Convert.ToBase64String(await HMACSHA512.HashDataAsync(_encoding.GetBytes((null as INodeSettings).AuthInfo.ThrowIfNull("Not authenticated").Guid), productNameStream, cancellationToken))
                    .Replace('/', '-')
                    .Replace('+', '_');
                return new ID_(id, container);
            }

            internal File_(string id, string container)
            {
                _file = new FileInfo(System.IO.Path.Combine(container, $"{id}{Extension}"));
                if (!_file.Exists) { using var _ = _file.Create(); }
                _file.Attributes |= FileAttributes.Hidden;
            }
        }
    }

    [JsonObject]
    public abstract record QSPreviews : IEnumerable<FileWithFormat>
    {
        internal static async Task<QSPreviews> GenerateAsync<QSPreviews>(IReadOnlyList<string> input, CancellationToken cancellationToken)
            where QSPreviews : RFProduct.QSPreviews
            => await (null as ITaskExecutor).ExecuteQS(input, new QSPreviewInfo(Guid.NewGuid().ToString()) { AlwaysGenerateQRPreview = true }, cancellationToken);


        public record Bound : QSPreviews
        {
            public RFProduct RFProduct { get; }
            public QSPreviews Preview { get; }

            internal static QSPreviews.Bound To(RFProduct product, QSPreviews previews)
            {
                foreach (var preview in previews)
                    preview.MoveTo(product, name: $"qs_{System.IO.Path.GetFileName(preview)}");
                return new(previews, product);
            }

            Bound(QSPreviews preview, RFProduct product)
            {
                Preview = preview;
                RFProduct = product;
            }

            public override IEnumerator<FileWithFormat> GetEnumerator() => Preview.GetEnumerator();
        }

        public abstract IEnumerator<FileWithFormat> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        { throw new NotImplementedException(); }
    }
}
