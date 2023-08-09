using _3DProductsPublish._3DModelDS;

namespace _3DProductsPublish.Tests;

public class _3DModelFixture
{
    internal _3DModel FromArchive => _3DModel.FromContainer(ArchivePath);
    internal readonly string ArchivePath;

    internal _3DModel FromDirectory => _3DModel.FromContainer(DirectoryPath);
    internal readonly string DirectoryPath;

    public _3DModelFixture()
    {
        string sourceCodeDirectoryPath = Directory.GetParent(Directory.GetCurrentDirectory())!
            .Parent!.Parent!.FullName;

        ArchivePath = Path.Combine(sourceCodeDirectoryPath, "test_product", "cambria_blender.zip");
        if (!File.Exists(ArchivePath))
            throw new ArgumentException($"{nameof(_3DModel)} archive container required for testing is missing.");
        DirectoryPath = Path.Combine(sourceCodeDirectoryPath, "test_product", "cambria_blender");
        if (!Directory.Exists(DirectoryPath))
            throw new ArgumentException($"{nameof(_3DModel)} directory container required for testing is missing.");
    }
}
