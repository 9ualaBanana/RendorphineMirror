using _3DProductsPublish._3DProductDS;
using Node.Tasks.Models;
using Node.Tasks.Models.ExecInfo;
using System.Collections;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using static Node.Listeners.TaskListener;

namespace MarkTM.RFProduct;

public interface IRFProductCreator
{
    static abstract ValueTask<RFProduct> RecognizeAsync(string input, string output, CancellationToken cancellationToken, bool disposeTemps = true);
}

public abstract partial record RFProduct : _3DProduct.AssetContainer, IRFProductCreator
{
    public string Idea { get; }
    public string ID { get; }
    public QSPreviews.Bound QSPreview { get; }

    readonly static Encoding _encoding = Encoding.UTF8;

    public static async ValueTask<RFProduct?> TryRecognizeAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
    {
        try { return await RecognizeAsync(idea, container, cancellationToken, disposeTemps); }
        catch { return null; }
    }

    // TODO: Check if RFProduct already exists and return already existing one.
    public static async ValueTask<RFProduct> RecognizeAsync(string idea, string container, CancellationToken cancellationToken, bool disposeTemps = true)
        => await RFProduct.Factory.CreateAsync(idea, container, CancellationToken.None, disposeTemps);

    protected RFProduct(string idea, string id, QSPreviews previews, string container, bool disposeTemps)
        : base(container, disposeTemps)
    {
        Idea = idea;
        ID = id;
        QSPreview = QSPreviews.Bound.To(this, previews);
        var idFile = new FileInfo(System.IO.Path.Combine(container, id));
        using var _ = idFile.Create();
        idFile.Attributes |= FileAttributes.Hidden;

        // Not sure if it supports archive containers.
        // TODO: Implement Copy & Move for AssetContainer.
        File.Copy(idea, System.IO.Path.Combine(container, System.IO.Path.GetFileName(idea)));
    }

    
    static class IDManager
    {
        internal static async Task<string> GenerateIDAsync(string productName, CancellationToken cancellationToken)
        {
            using var productNameStream = new MemoryStream(_encoding.GetBytes(productName));
            return Convert.ToBase64String(await HMACSHA512.HashDataAsync(_encoding.GetBytes(Node.Settings.Guid), productNameStream, cancellationToken))
                .Replace('/', '-')
                .Replace('+', '_');
        }
    }

    public abstract record QSPreviews : IEnumerable<FileWithFormat>
    {
        internal static async Task<QSPreviews> GenerateAsync<QSPreviews>(IReadOnlyList<string> input, CancellationToken cancellationToken)
            where QSPreviews : RFProduct.QSPreviews
            => (await
            (await Api.GlobalClient.PostAsJsonAsync(
                $"http://localhost:{Node.Settings.LocalListenPort}/tasks/executeqsp",
                new QSPreviewTaskExecutionInfo(input, new QSPreviewInfo(Guid.NewGuid().ToString()) { AlwaysGenerateQRPreview = true }), cancellationToken))
            .GetJsonIfSuccessfulAsync($"{nameof(QSPreviews)} generation for {nameof(RFProduct)} failed."))
            ["value"]?.ToObject<QSPreviews>() ?? throw new InvalidCastException($"{nameof(QSPreviews)} generation endpoint returned data in a wrong format.");


        public record Bound : QSPreviews
        {
            public RFProduct RFProduct { get; }
            public QSPreviews Preview { get; }

            internal static QSPreviews.Bound To(RFProduct RFProduct, QSPreviews QSPreview)
            {
                foreach (var preview in QSPreview)
                    preview.MoveTo(RFProduct.Path);
                return new(QSPreview, RFProduct);
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
