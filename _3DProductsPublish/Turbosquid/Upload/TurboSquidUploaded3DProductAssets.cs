using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload.Processing;

namespace _3DProductsPublish.Turbosquid.Upload;

internal record TurboSquidUploaded3DProductAssets(
    IEnumerable<ITurboSquidProcessed3DProductAsset<TurboSquid3DModel>> Models,
    IEnumerable<ITurboSquidProcessed3DProductAsset<TurboSquid3DProductThumbnail>> Thumbnails)
{
}
