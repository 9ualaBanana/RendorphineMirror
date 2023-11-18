using _3DProductsPublish._3DProductDS;
using FluentAssertions;

namespace _3DProductsPublish.Tests;

public class _3DModelTest
{
    public class Archive : IClassFixture<_3DModelFixture>
    {
        readonly _3DModelFixture _3DModelFixture;

        public Archive(_3DModelFixture _3DModelFixture)
        {
            this._3DModelFixture = _3DModelFixture;
        }

        [Fact]
        public void Initialization()
        {
            using var model = _3DModelFixture.FromArchive;

            model.OriginalContainer.Should().Be(_3DModel.ContainerType.Archive);
            model.OriginalPath.Should().Be(_3DModelFixture.ArchivePath);
        }
    }

    public class Directory : IClassFixture<_3DModelFixture>
    {
        readonly _3DModelFixture _3DModelFixture;

        public Directory(_3DModelFixture _3DModelFixture)
        {
            this._3DModelFixture = _3DModelFixture;
        }

        [Fact]
        public void Initidalization()
        {
            using var model = _3DModel.FromContainer(_3DModelFixture.DirectoryPath);

            model.OriginalContainer.Should().Be(_3DModel.ContainerType.Directory);
            model.OriginalPath.Should().Be(_3DModelFixture.DirectoryPath);
        }
    }
}