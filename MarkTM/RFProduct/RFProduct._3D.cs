using Node.Common.Models;
using Node.Tasks.Exec.Output;
using Node.Tasks.Models;
using SixLabors.ImageSharp;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record _3D : RFProduct
    {
        public record Constructor : Constructor<Idea_, QSPreviews, _3D>
        {
            internal override _3D Create(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
                => new(idea, id, previews, container);

            protected override ValueTask<string> GetPreviewInputAsync(Idea_ idea) => ValueTask.FromResult(idea.Renders.First());
            internal override string[] SubProductsIdeas(Idea_ idea) => idea.Renders.ToArray();
        }
        _3D(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
            : base(idea, id, previews, container)
        {
        }


        new public record Idea_
            : RFProduct.Idea_
        {
            internal AssetContainer Container => new(Path);
            [JsonIgnore] public IEnumerable<string> Assets => AssetsInside(Container);

            [JsonIgnore] public IEnumerable<string> Packages => Assets.Where(IsPackage);
            [JsonIgnore] public string Metadata => Assets.Single(IsMetadata);
            [JsonIgnore] public IEnumerable<string> Renders => Assets.Where(IsRender);
            [JsonIgnore] public string Sales
            {
                get
                {
                    if (Assets.SingleOrDefault(_ => _.EndsWith("_Sales.json")) is string sales)
                        return sales;
                    else { using var _ = File.Create($"{System.IO.Path.Combine(Path, System.IO.Path.GetFileNameWithoutExtension(Packages.First()))}_Sales.json"); return _.Name; }
                }
            }
            [JsonIgnore] public string TSMeta => Assets.Single(_ => System.IO.Path.GetFileName(_) == "turbosquid.meta");

            [JsonConstructor]
            Idea_(string path)
                : this(new AssetContainer(path))
            {
            }
            internal Idea_(AssetContainer container)
                : base(container)
            {
            }

            public record Recognizer : IRecognizer<Idea_>
            {
                public Idea_? TryRecognize(string idea)
                {
                    if (AssetContainer.Exists(idea) && new AssetContainer(idea) is AssetContainer ideaContainer
                        && AssetsInside(ideaContainer) is IEnumerable<string> assets
                            && assets.Any(IsPackage)
                            && assets.Single(IsMetadata) is not null
                            && assets.Count(IsRender) >= 7
                            && assets.Any(IsSettings))
                        return new(ideaContainer);
                    return null;
                }
            }

            static IEnumerable<string> AssetsInside(AssetContainer container) => container.EnumerateEntries(EntryType.NonContainers);

            static bool IsPackage(string asset) => _packageExtensions.Contains(System.IO.Path.GetExtension(asset));
            readonly static HashSet<string> _packageExtensions = [".UnityPackage"];

            public static bool IsWireframe(string asset) => _wireframeSuffixes.Any(wireframeSuffix => System.IO.Path.GetFileNameWithoutExtension(asset).EndsWith(wireframeSuffix));
            readonly static HashSet<string> _wireframeSuffixes = ["_vp", "_wire"];

            public static bool IsRender(string asset) => _renderSuffixes.Any(renderSuffix => System.IO.Path.GetFileNameWithoutExtension(asset).EndsWith(renderSuffix));
            readonly static HashSet<string> _renderSuffixes = new(_wireframeSuffixes.Union(["_screenshot"]));

            static bool IsMetadata(string asset) => asset.EndsWith("_Submit.json");

            static bool IsSettings(string asset) => asset.EndsWith("_Settings.ini");
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
