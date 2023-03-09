namespace Telegram.MediaFiles;

/// <summary>
/// Represents <see cref="MediaFile"/> stored inside <see cref="MediaFilesCache"/>.
/// </summary>
/// <remarks>
/// Physical copy of the file represented by this <see cref="CachedMediaFile"/> which is stored at <see cref="Path"/>
/// is deleted after <see cref="MediaFilesCache.StorageTimeAfterRetrieval"/> when retrieved via <see cref="MediaFilesCache.RetrieveMediaFileWith(string)"/>.
/// </remarks>
public record CachedMediaFile(MediaFile File, string Index, string Path)
{
}
