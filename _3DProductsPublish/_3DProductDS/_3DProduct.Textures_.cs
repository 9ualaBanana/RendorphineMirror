using MarkTM.RFProduct;

namespace _3DProductsPublish._3DProductDS;

public partial record _3DProduct
{
    public record Textures_(string Path) : I3DProductAsset
    {
        internal static IEnumerable<Textures_> EnumerateAt(string _3DProductDirectory)
            => Directory.EnumerateFiles(_3DProductDirectory).Where(RFProduct._3D.Idea_.IsTextures)
            .Select(_ => new Textures_(_));
    }
}
