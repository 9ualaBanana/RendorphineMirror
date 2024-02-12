namespace _3DProductsPublish._3DProductDS;

public partial record _3DProduct
{
    public record Textures_ : AssetContainer
    {
        internal const string ContainerName = "textures.zip";

        internal static Textures_? FindAt(string _3DProductDirectory, bool disposeTemps = true)
        {
            var path = System.IO.Path.Combine(_3DProductDirectory, Textures_.ContainerName);
            return Exists(path) ? new(path, disposeTemps) : null;
        }

        protected Textures_(string path, bool disposeTemps = true)
            : base(path, disposeTemps)
        {
        }

        new internal IEnumerable<Texture_> EnumerateFiles()
            => base.EnumerateEntries()
            .Where(Texture_.HasValidExtension)
            .Select(_ => new Texture_(_));
    }

    internal record Texture_ : I3DProductAsset
    {
        internal string Path { get; }
        public string Name { get; }
        public long Size
        {
            get
            {
                if (_size is null)
                {
                    using var fileStream = File.OpenRead(Path);
                    _size = fileStream.Length;
                }

                return _size.Value;
            }
        }
        long? _size;

        public Texture_(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(Path);
        }

        internal static bool HasValidExtension(string pathOrExtension) =>
            _validExtensions.Contains(System.IO.Path.GetExtension(pathOrExtension));

        readonly static string[] _validExtensions = { ".jpeg", ".jpg", ".png" };
    }
}
