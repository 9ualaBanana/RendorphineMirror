using Node.Common.Models;
using FluentAssertions;

namespace _3DProductsPublish.Tests;

public class _3DModelArchiverTest : IClassFixture<_3DModelFixture>
{
    readonly _3DModelFixture _3DModelFixture;

    public _3DModelArchiverTest(_3DModelFixture _3DModelFixture)
    {
        this._3DModelFixture = _3DModelFixture;
    }

    [Fact]
    public async Task Archive_3DModelFromArchiveContainer()
    {
        using var model = _3DModelFixture.FromArchive;

        string archivedModelPath = await model.Archive();

        archivedModelPath.Should().Be(model.Path);
        File.Exists(archivedModelPath).Should().BeTrue();
    }

    [Fact]
    public async Task Archive_3DModelFromDirectoryContainer()
    {
        using var model = _3DModelFixture.FromDirectory;

        string archivedModelPath = await model.Archive();

        archivedModelPath.Should().NotBe(model.Path);
        File.Exists(archivedModelPath).Should().BeTrue();

        File.Delete(archivedModelPath);
    }

    [Fact]
    public void Unpack_3DModelFromArchiveContainer()
    {
        using var model = _3DModelFixture.FromArchive;

        string unpackedModelPath = AssetContainer.Archive_.Unpack(model.Path);

        unpackedModelPath.Should().NotBe(model.Path);
        Directory.Exists(unpackedModelPath).Should().BeTrue();
    }

    [Fact]
    public void Unpack_3DModelFromDirectoryContainer()
    {
        using var model = _3DModelFixture.FromDirectory;

        var unpackingDirectory = () => AssetContainer.Archive_.Unpack(model.Path);

        unpackingDirectory.Should().Throw<FileNotFoundException>();
        Directory.Exists(model.Path).Should().BeTrue();
    }
}