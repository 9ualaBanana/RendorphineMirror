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

        // Not all statuses might be supported, which might result in parsing exception.
        public enum Status
        {
            none,
            draft,
            awaiting_review,
            suspended_awaiting_review,
            online
        }


        new public record Idea_
            : RFProduct.Idea_
        {
            internal AssetContainer Container => new(Path);
            [JsonIgnore] public IEnumerable<string> Assets => AssetsInside(Container);

            [JsonIgnore] public IEnumerable<string> Packages => Assets.Where(IsPackage);
            [JsonIgnore] public string Metadata => Assets.Single(IsMetadata);
            [JsonIgnore] public IEnumerable<string> Renders => Assets.Where(IsRender);
            [JsonIgnore] public IEnumerable<string> Textures => Assets.Where(IsTextures);
            [JsonIgnore] public string Sales
            {
                get
                {
                    if (Assets.SingleOrDefault(_ => _.EndsWith("_Sales.json")) is string sales)
                        return sales;
                    else { using var _ = File.Create($"{System.IO.Path.Combine(Path, System.IO.Path.GetFileNameWithoutExtension(Packages.First()))}_Sales.json"); return _.Name; }
                }
            }
            [JsonIgnore] public Status Status
            {
                get => File.ReadAllText(StatusFile) is string content && content.Length is not 0 
                    && Enum.TryParse<Status>(JObject.Parse(content)["status"]?.Value<string>(), true, out var status) ?
                    status : Status = Status.none;
                set => File.WriteAllText(StatusFile, JsonConvert.SerializeObject(new { status = value.ToStringInvariant() }));
            }
            string StatusFile
            {
                get
                {
                    if (Assets.SingleOrDefault(_ => _.EndsWith("_Status.json")) is string status)
                        return status;
                    else { using var _ = File.Create($"{System.IO.Path.Combine(Path, System.IO.Path.GetFileNameWithoutExtension(Packages.First()))}_Status.json"); return _.Name; }
                }
            }

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
                        assets.Any(IsWireframe) ?
                        assets.Count(IsProductShot) is int productShotsCount && productShotsCount >= RequiredProductShotsCount ?
                        //assets.Any(IsSettings)

                    new(ideaContainer)

                        : throw new FileNotFoundException($"{typeof(_3D.Idea_).FullName} must contain at least {RequiredProductShotsCount} product shots and only {productShotsCount} were found.")
                        : throw new FileNotFoundException($"{typeof(_3D.Idea_).FullName} must contain at least 1 wireframe image with any of the following suffixes [{string.Join(',', _wireframeSuffixes)}]")
                        : throw new FileNotFoundException($"{typeof(_3D.Idea_).FullName} must contain metadata file [{_metadataSuffix}].")
                        : throw new FileNotFoundException($"{typeof(_3D.Idea_).FullName} must contain at least 1 package.")
                    : throw new InvalidDataException($"{idea} doesn't represent {typeof(_3D).FullName}.");

                const int RequiredProductShotsCount = 5;
            }

            static IEnumerable<string> AssetsInside(AssetContainer container) => container.EnumerateEntries();

            public static bool IsPackage(string asset) => _packageSuffixes.Any(asset.EndsWith);
            readonly static HashSet<string> _packageSuffixes = [".UnityPackage", ".zip"];

            static bool IsMetadata(string asset) => asset.EndsWith(_metadataSuffix);
            readonly static string _metadataSuffix = "_Submit.json";

            public static bool IsWireframe(string asset) =>
                File.Exists(asset) &&
                _wireframeSuffixes.Any(wireframeSuffix => System.IO.Path.GetFileNameWithoutExtension(asset).EndsWith(wireframeSuffix));
            readonly static HashSet<string> _wireframeSuffixes = ["_vp", "_wire"];

            public static bool IsProductShot(string asset)
                => System.IO.Path.GetExtension(asset).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg"
                && !System.IO.Path.GetFileNameWithoutExtension(asset).EndsWith("_preview")
                && _wireframeSuffixes.All(wireframeSuffix => !System.IO.Path.GetFileNameWithoutExtension(asset).EndsWith(wireframeSuffix));
            //static bool IsProductShot(string asset) => _productShotSuffixes.Any(productShotSuffix => !System.IO.Path.GetFileNameWithoutExtension(asset).EndsWith("_preview"));
            //readonly static HashSet<string> _productShotSuffixes = ["_screenshot"];

            public static bool IsRender(string asset) => IsWireframe(asset) || IsProductShot(asset);

            public static bool IsTextures(string asset) => _texturesSuffixes.Any(textureSuffix => System.IO.Path.GetFileName(asset).EndsWith(textureSuffix));
            readonly static HashSet<string> _texturesSuffixes = ["_Textures.zip"];

            //static bool IsSettings(string asset) => asset.EndsWith("_Settings.ini");
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
