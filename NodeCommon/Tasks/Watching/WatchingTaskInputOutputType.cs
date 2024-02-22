namespace NodeCommon.Tasks.Watching;

[JsonConverter(typeof(StringEnumConverter))]
public enum WatchingTaskInputType
{
    Local,
    MPlus,
    MPlusAllFiles,
    OtherNode,
    RectReleases,
    OneClick,
    Generate3DRFProduct,
}
[JsonConverter(typeof(StringEnumConverter))]
public enum WatchingTaskOutputType
{
    Torrent,
    MPlus,
    QSPreview,
}
