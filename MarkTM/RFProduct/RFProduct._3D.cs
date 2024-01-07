﻿using Node.Tasks.Exec.Output;
using Node.Tasks.Models;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record _3D : RFProduct
    {
        public record Constructor : Constructor<Idea_, QSPreviews, _3D>
        {

            internal override async Task<_3D> CreateAsync(Idea_ idea, string id, AssetContainer container, CancellationToken cancellationToken)
                => new _3D(idea, id, await QSPreviews.GenerateAsync(@"C:\Users\9uala\OneDrive\Documents\oc\input\btrfl.jpg", container, cancellationToken), container);

            protected override async Task<RFProduct[]> CreateSubProductsAsync(_3D product, Factory factory, CancellationToken cancellationToken)
            {
                var renders = new AssetContainer(((Idea_)product.Idea).Renders);
                return [await factory.CreateAsync(renders, renders, cancellationToken)];
            }
        }
        _3D(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
            : base(idea, id, previews, container)
        {
        }


        new public record Idea_
            : RFProduct.Idea_
        {
            internal AssetContainer Container => new(Path);
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

            [JsonConstructor]
            Idea_(string path)
                : this(new AssetContainer(path))
            {
            }
            internal Idea_(AssetContainer container)
                : base(container)
            {
            }

            static AssetContainer? _3DAssetsInside(AssetContainer dataContainer)
                => dataContainer.EnumerateEntries(EntryType.Containers).SingleOrDefault(_ => System.IO.Path.GetFileName(_) != "OneClickImport")
                is string assetsContainer ? new(assetsContainer) : null;

            public record Recognizer : IRecognizer<Idea_>
            {
                public Idea_? TryRecognize(string idea)
                {
                    if (AssetContainer.Exists(idea) && new AssetContainer(idea) is AssetContainer ideaContainer
                        && _3DAssetsInside(ideaContainer) is AssetContainer assetsContainer
                        && assetsContainer.EnumerateEntries(EntryType.NonContainers)
                            .SingleOrDefault(_ => System.IO.Path.GetFileName(_) == $"{assetsContainer.Name}.txt") is string info && File.Exists(info)
                        && assetsContainer.EnumerateEntries().Select(_ => System.IO.Path.GetFileName(_)) is IEnumerable<string> assets
                            && assets.FirstOrDefault(_ => _ == renders) is string renders_ && Directory.EnumerateFiles(System.IO.Path.Combine(assetsContainer, renders_)).Any()
                            && assets.Any(_ => _ == textures)
                            && assets.Any(_ => _ == meshes))
                        return new(ideaContainer);
                    return null;
                }
            }
        }

        public record Renders : RFProduct
        {
            public record Constructor : Constructor<Idea_, _3D.QSPreviews, Renders>
            {
                internal override async Task<Renders> CreateAsync(Idea_ idea, string id, AssetContainer container, CancellationToken cancellationToken)
                    => new Renders(idea, id, await QSPreviews.GenerateAsync(@"C:\Users\9uala\OneDrive\Documents\oc\input\btrfl.jpg", container, cancellationToken), container);
                // TODO: Implement QSPreviews properly.
            }
            protected Renders(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
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
                    public Idea_? TryRecognize(string idea)
                        => new DirectoryInfo(idea).Parent?.Parent?.FullName is string parentIdea && Parent.TryRecognize(parentIdea) is not null ?
                        new(idea) : null;

                    public required _3D.Idea_.Recognizer Parent { get; init; }
                }
            }
        }

        new public record QSPreviews(
            [JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR) : RFProduct.QSPreviews
        {
            public record Generator : Generator<QSPreviews>
            {
            }

            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR }.AsEnumerable().GetEnumerator();
        }
    }
}
