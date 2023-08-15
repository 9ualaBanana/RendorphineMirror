namespace NodeCommon.Tasks.Model;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskInputType
{
    Stub,
    MPlus,
    DownloadLink,
    Torrent,
    User,
    DirectUpload,
    TitleKeywords,
}
[JsonConverter(typeof(StringEnumConverter))]
public enum TaskOutputType
{
    MPlus,
    Torrent,
    User,
    QSPreview,
    DirectDownload,
    TitleKeywords,
}