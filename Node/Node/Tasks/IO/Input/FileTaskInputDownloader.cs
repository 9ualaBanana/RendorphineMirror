using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Node.Tasks.IO.Input;

public abstract class FileTaskInputDownloader<TInput> : TaskInputDownloader<TInput, ReadOnlyTaskFileList>
    where TInput : ITaskInputInfo
{
    public required ITaskInputDirectoryProvider TaskDirectoryProvider { get; init; }

    public override async Task<ReadOnlyTaskFileList> Download(TInput input, TaskObject obj, CancellationToken token)
    {
        var result = await base.Download(input, obj, token);

        // fix jpegs that are rotated using metadata which doesn't do well with some tools
        foreach (var jpeg in result.Where(f => f.Format == FileFormat.Jpeg).ToArray())
        {
            using var img = Image.Load<Rgba32>(jpeg.Path);
            if (img.Metadata.ExifProfile?.TryGetValue(ExifTag.Orientation, out var exif) == true && exif is not null)
            {
                img.Mutate(ctx => ctx.AutoOrient());
                await img.SaveAsJpegAsync(jpeg.Path, new JpegEncoder() { Quality = 100 }, token);
            }
        }

        return result;
    }
}
