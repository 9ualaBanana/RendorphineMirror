using System.Collections;

namespace Node.Tasks.Exec.Output;

[JsonObject]
public class QSPreviewOutput : IEnumerable<FileWithFormat>
{
    public FileWithFormat? ImageFooter { get; private set; }
    public FileWithFormat? ImageQr { get; private set; }
    public FileWithFormat? Video { get; private set; }
    readonly string Directory;

    public QSPreviewOutput(string directory) => Directory = directory;

    public FileWithFormat InitializeImageFooter() => ImageFooter ??= TaskFileList.NewFile(Directory, FileFormat.Jpeg, "pjfooter");
    public FileWithFormat InitializeImageQr() => ImageQr ??= TaskFileList.NewFile(Directory, FileFormat.Jpeg, "pjqr");
    public FileWithFormat InitializeVideo() => Video ??= TaskFileList.NewFile(Directory, FileFormat.Jpeg, "pm");

    public IEnumerator<FileWithFormat> GetEnumerator()
    {
        if (ImageFooter is not null) yield return ImageFooter;
        if (ImageQr is not null) yield return ImageQr;
        if (Video is not null) yield return Video;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
