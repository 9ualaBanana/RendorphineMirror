using _3DProductsPublish._3DProductDS;
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
    public void Archive_3DModelFromArchiveContainer()
    {
        using var model = _3DModelFixture.FromArchive;

        string archivedModelPath = _3DModelArchiver.Archive(model);

        archivedModelPath.Should().Be(model.OriginalPath);
        File.Exists(archivedModelPath).Should().BeTrue();
    }

    [Fact]
    public void Archive_3DModelFromDirectoryContainer()
    {
        using var model = _3DModelFixture.FromDirectory;

        string archivedModelPath = _3DModelArchiver.Archive(model);

        archivedModelPath.Should().NotBe(model.OriginalPath);
        File.Exists(archivedModelPath).Should().BeTrue();

        File.Delete(archivedModelPath);
    }

    [Fact]
    public void Unpack_3DModelFromArchiveContainer()
    {
        using var model = _3DModelFixture.FromArchive;

        string unpackedModelPath = _3DModelArchiver.Unpack(model);

        unpackedModelPath.Should().NotBe(model.OriginalPath);
        Directory.Exists(unpackedModelPath).Should().BeTrue();
    }

    [Fact]
    public void Unpack_3DModelFromDirectoryContainer()
    {
        using var model = _3DModelFixture.FromDirectory;

        string unpackedModelPath = _3DModelArchiver.Unpack(model);

        unpackedModelPath.Should().Be(model.OriginalPath);
        Directory.Exists(unpackedModelPath).Should().BeTrue();
    }
}