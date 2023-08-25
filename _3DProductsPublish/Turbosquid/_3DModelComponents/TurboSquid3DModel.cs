using _3DProductsPublish._3DProductDS;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

internal partial class TurboSquid3DModel : _3DModel
{
    internal readonly Metadata Metadata_;

    internal TurboSquid3DModel(_3DModel _3DModel, Metadata metadata)
        : base(_3DModel)
    {
        Metadata_ = metadata;
    }

    protected TurboSquid3DModel(TurboSquid3DModel _3DModel)
        : base(_3DModel)
    {
        Metadata_ = _3DModel.Metadata_;
    }
}
