using Node.Common.Models;
using Node.Tasks.Exec.Output;
using Node.Tasks.Models;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record WebGL : RFProduct
    {
        public record Constructor : Constructor<Idea_, QSPreviews, WebGL>
        {
            internal override WebGL Create(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
                => new(idea, id, previews, container);

            protected override ValueTask<string> GetPreviewInputAsync(Idea_ idea) => ValueTask.FromResult(idea.Path);
        }
        WebGL(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
            : base(idea, id, previews, container)
        {
        }


        new public record Idea_
            : RFProduct.Idea_
        {
            Idea_(string path)
                : base(path)
            {
            }
            public record Recognizer : IRecognizer<Idea_>
            {
                public Idea_ Recognize(string idea) =>
                    AssetContainer.Exists(idea) && new AssetContainer(idea) is AssetContainer ideaContainer
                    && ideaContainer.EnumerateEntries() is IEnumerable<string> assets ?
                        assets.SingleOrDefault(IsIndex) is not null ?
                        assets.SingleOrDefault(IsBuild) is not null ?

                    new Idea_(idea)

                        : throw new FileNotFoundException($"{typeof(WebGL.Idea_)} must contain build container.")
                        : throw new FileNotFoundException($"{typeof(WebGL.Idea_)} must contain index.html file.")
                    : throw new InvalidDataException($"{idea} doesn't represent {typeof(WebGL).FullName}.");

                static bool IsIndex(string asset) => File.Exists(asset)
                    && System.IO.Path.GetFileName(asset) == "index.html";

                static bool IsBuild(string asset) => Directory.Exists(asset)
                    && Directory.EnumerateFiles(asset).Select(System.IO.Path.GetFileName).All(_ => _.StartsWith("WebGL"));
            }
        }

        new public record QSPreviews(
            [property: JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [property: JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR) : RFProduct.QSPreviews
        {
            public record Generator : Generator<QSPreviews>;

            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR }.AsEnumerable().GetEnumerator();
        }
    }
}
