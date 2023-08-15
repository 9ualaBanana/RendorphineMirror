using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;

namespace _3DProductsPublish.Turbosquid.Upload;

internal record TurboSquidUploaded3DProductAssets(
    IEnumerable<ITurboSquidProcessed3DProductAsset<_3DModel>> Models,
    IEnumerable<ITurboSquidProcessed3DProductAsset<TurboSquid3DProductThumbnail>> Thumbnails)
{
}
