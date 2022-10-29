using Transport.Upload._3DModelsUpload.Models;

namespace Transport.Upload._3DModelsUpload.Services;

public static class _3DModelUploader
{
    public async Task UploadAsync(_3DModel _3DModel) =>
        await UploadAsync(new Composite3DModel(previews: null, _3DModel._ToModelParts()));

    public async Task UploadAsync(Composite3DModel composite3DModel)
    {
        composite3DModel.Archive();

    protected abstract Task UploadAsyncCore(Composite3DModel composite3DModel);
    }
}
