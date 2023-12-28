using Node.Tasks.Exec.Output;
using Node.Tasks.Models;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record _3D : RFProduct
    {
        public record Constructor : Constructor<_3D>
        {
            internal override async Task<_3D> CreateAsync(string idea, ID_ id, AssetContainer container, CancellationToken cancellationToken)
            {
                // TODO: Dispose of that container.
                var ideaContainer = new AssetContainer(idea);

                var _3d = new _3D(Idea_.Inside(ideaContainer)!, id, await QSPreviews.GenerateAsync(@"C:\Users\9uala\OneDrive\Documents\oc\input\btrfl.jpg", container, cancellationToken), container);
                // Extract to the factory or Constructor as file Ideas of subproducts will have to be moved to their corresponding RFProduct AssetContainer.
                _3d.Store(ideaContainer, @as: Idea_.FileName, StoreMode.Move);
                return _3d;
                //if (data.Container.ContainerType is Type_.Archive)
                //{
                //    var relativePath = System.IO.Path.GetRelativePath(System.IO.Path.GetTempPath(), idPath);
                //    // Stores ID file in the input container. Preferably store only inside the resulting container. This used to be the default behavior in the factory I believe.
                //    data.Container.Store(
                //        idPath,
                //        @as: relativePath[(relativePath.IndexOf(System.IO.Path.DirectorySeparatorChar) + 1)..]);
                //}
            }
            protected override async Task<RFProduct[]> CreateSubProductsAsync(_3D product, Factory factory, CancellationToken cancellationToken)
            {
                var renders = new AssetContainer(((Idea_)product.Idea).Renders);
                return [await factory.CreateAsync<Renders>(renders, await factory.ID.AssignedTo(renders, cancellationToken), renders, cancellationToken)];
            }
            public required QSPreviews.Generator QSPreviews { get; init; }
            public required ID_.Generator ID { get; init; }
        }
        _3D(Idea_ idea, ID_ id, QSPreviews previews, AssetContainer container)
            : base(idea, id, previews, container)
        {
        }


        new public record Idea_
            : RFProduct.Idea_
        {
            internal AssetContainer Container;
            AssetContainer Assets => _3DAssetsInside(Container) ??
                throw new InvalidOperationException($"{nameof(AssetContainer)} with {nameof(_3D)} assets went missing.");

            // TODO: Change to AssetContainers as soon as implement IDisposable for them.
            [JsonIgnore] public string Renders => System.IO.Path.Combine(Assets, renders);
            const string renders = nameof(renders);
            [JsonIgnore] public string Textures => System.IO.Path.Combine(Assets, textures);
            const string textures = nameof(textures);
            [JsonIgnore] public string Meshes => System.IO.Path.Combine(Assets, meshes);
            const string meshes = nameof(meshes);
            [JsonIgnore] public string ExportInfo => System.IO.Path.Combine(Assets, $"{Assets.Name}.txt");

            internal static Idea_? Inside(AssetContainer dataContainer)
            {
                //if (dataContainer.Name.Contains("[MaxOcExport]"))
                //{
                    // TODO: Dispose of that container in the appropriate place.
                    var assetsContainer = _3DAssetsInside(dataContainer);
                    if (assetsContainer is not null
                        && assetsContainer.EnumerateEntries(EntryType.NonContainers)
                            .SingleOrDefault(_ => System.IO.Path.GetFileName(_) == $"{assetsContainer.Name}.txt") is string info && File.Exists(info)
                        && assetsContainer.EnumerateEntries().Select(_ => System.IO.Path.GetFileName(_)) is IEnumerable<string> assets
                            && assets.Any(_ => _ == renders)
                            && assets.Any(_ => _ == textures)
                            && assets.Any(_ => _ == meshes))
                    return new(dataContainer);
                //}
                return null;
            }

            [JsonConstructor]
            Idea_(string path)
                : this(new AssetContainer(path))
            {
            }
            Idea_(AssetContainer container)
                : base(container)
            {
                Container = container;
            }

            static AssetContainer? _3DAssetsInside(AssetContainer dataContainer)
                => dataContainer.EnumerateEntries(EntryType.Containers).SingleOrDefault(_ => System.IO.Path.GetFileName(_) != "OneClickImport")
                is string assetsContainer ? new(assetsContainer) : null;
        }

        public record Renders : RFProduct
        {
            public record Constructor : Constructor<Renders>
            {
                internal override async Task<Renders> CreateAsync(string idea, ID_ id, AssetContainer container, CancellationToken cancellationToken)
                    => new(idea, id, await QSPreviews.GenerateAsync(@"C:\Users\9uala\OneDrive\Documents\oc\input\btrfl.jpg", container, cancellationToken), container);
                // TODO: Implement QSPreviews properly.
                public required _3D.QSPreviews.Generator QSPreviews { get; init; }
            }
            protected Renders(string idea, ID_ id, QSPreviews previews, AssetContainer container)
                : base(new(idea), id, previews, container)
            {
            }
        }

        new public record QSPreviews(
            [JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR) : RFProduct.QSPreviews
        {
            public record Generator : Generator<_3D.QSPreviews>
            {
            }

            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR }.AsEnumerable().GetEnumerator();
        }
    }
}
