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
                public Idea_ Recognize(string idea) =>
                    AssetContainer.Exists(idea) && new AssetContainer(idea) is AssetContainer ideaContainer
                    && AssetsInside(ideaContainer) is IEnumerable<string> assets ?
                        assets.Any(IsPackage) ?
                        assets.SingleOrDefault(IsMetadata) is not null ?
                        assets.Count(IsRender) is int count && count >= RequiredRendersCount ?
                        assets.Any(IsWireframe) ?
                        //assets.Any(IsSettings)

                    new(ideaContainer)

                        : throw new FileNotFoundException($"{typeof(_3D.Idea_).FullName} must contain at least 1 wireframe render with any of the following suffixes [{string.Join(',', _wireframeSuffixes)}]")
                        : throw new FileNotFoundException($"{typeof(_3D.Idea_).FullName} must contain at least {RequiredRendersCount} renders and only {count} were found.")
                        : throw new FileNotFoundException($"{typeof(_3D.Idea_).FullName} must contain metadata file [{_metadataSuffix}].")
                        : throw new FileNotFoundException($"{typeof(_3D.Idea_).FullName} must contain at least 1 package.")
                    : throw new InvalidDataException($"{idea} doesn't represent {typeof(_3D).FullName}.");

                const int RequiredRendersCount = 7;
            }

            static IEnumerable<string> AssetsInside(AssetContainer container) => container.EnumerateEntries(EntryType.NonContainers);

            static bool IsPackage(string asset) => _packageExtensions.Contains(System.IO.Path.GetExtension(asset));
            readonly static HashSet<string> _packageExtensions = [".UnityPackage"];

            public static bool IsWireframe(string asset) => _wireframeSuffixes.Any(wireframeSuffix => System.IO.Path.GetFileNameWithoutExtension(asset).EndsWith(wireframeSuffix));
            readonly static HashSet<string> _wireframeSuffixes = ["_vp", "_wire"];

            public static bool IsRender(string asset) => _renderSuffixes.Any(renderSuffix => System.IO.Path.GetFileNameWithoutExtension(asset).EndsWith(renderSuffix));
            readonly static HashSet<string> _renderSuffixes = new(_wireframeSuffixes.Union(["_screenshot"]));

            static bool IsMetadata(string asset) => asset.EndsWith(_metadataSuffix);
            readonly static string _metadataSuffix = "_Submit.json";

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
