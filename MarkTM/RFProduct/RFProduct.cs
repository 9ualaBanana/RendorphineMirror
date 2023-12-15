using Node.Tasks;
using Node.Tasks.Models;
using Node.Tasks.Models.ExecInfo;
using NodeToUI;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct : AssetContainer
{
    public ID_ ID { get; }
    public QSPreviews QSPreview { get; }

    readonly static Encoding _encoding = Encoding.UTF8;

    [JsonConstructor]
    protected RFProduct(ID_ id, QSPreviews previews, AssetContainer container)
        : base(container)
    {
        ID = id;
        QSPreview = previews;
        QSPreview.BindTo(this);
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
                => container.EnumerateFiles(FilesToEnumerate.NonContainers)
                .SingleOrDefault(_ => System.IO.Path.GetExtension(_) == File_.Extension) is string file ?

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
        public record Generator<QSPreviews>
            where QSPreviews : RFProduct.QSPreviews
        {
            public required INodeSettings NodeSettings { get; init; }
            public required ITaskExecutor TaskExecutor { get; init; }

            internal async Task<QSPreviews> GenerateAsync(string idea, AssetContainer container, CancellationToken cancellationToken)
            {
                var qsOutput = await TaskExecutor.ExecuteQS(
                    await PrepareInputAsync(idea, container, cancellationToken),
                    new QSPreviewInfo(Guid.NewGuid().ToString()) { AlwaysGenerateQRPreview = true },
                    cancellationToken);
                return JObject.FromObject(qsOutput).ToObject<QSPreviews>() ??
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

    static class Idea
    {
        internal static bool Exists(string idea)
            => File.Exists(idea) && System.IO.Path.GetFileNameWithoutExtension(idea) == FileName;

        internal const string FileName = "idea";
    }
}
