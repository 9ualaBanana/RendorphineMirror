using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace Telegram.Infrastructure.MediaFiles;

/// <summary>
/// Represents <see cref="MediaFile"/> stored inside <see cref="MediaFilesCache"/>.
/// </summary>
/// <remarks>
/// Physical copy of the file represented by this <see cref="CachedMediaFile"/> which is stored at <see cref="Path"/>
/// is deleted after <see cref="MediaFilesCache.CachingTime"/> when retrieved via <see cref="MediaFilesCache.TryRetrieveMediaFileWith(string)"/>.
/// </remarks>
public record CachedMediaFile(MediaFile File, Guid Index, string Path)
{
    internal long Size => _size ??= new FileInfo(Path).Length;
    long? _size;

    /// <summary>
    /// Intended for use only with AutoStorage to construct objects with a property/properties
    /// playing role of a primary key based on which the search is performed.
    /// </summary>
    /// <param name="index">The property that plays the role of a primary key based on which the search is performed.</param>
    internal static CachedMediaFile WithIndex(Guid index)
        => new(default!, index, default!);

    public class IndexEqualityComparer : IEqualityComparer<AutoStorageItem<CachedMediaFile>>
    {
        public bool Equals(AutoStorageItem<CachedMediaFile>? x, AutoStorageItem<CachedMediaFile>? y)
            => x?.Value.Index == y?.Value.Index;

        public int GetHashCode([DisallowNull] AutoStorageItem<CachedMediaFile> obj)
            => obj.Value.Index.GetHashCode();
    }
}
