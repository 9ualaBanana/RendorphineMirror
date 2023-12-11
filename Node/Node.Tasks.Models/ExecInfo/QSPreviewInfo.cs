namespace Node.Tasks.Models.ExecInfo;

public class QSPreviewInfo
{
    [JsonProperty("qid")]
    public string Qid { get; }

    public bool AlwaysGenerateQRPreview { get; init; } = false;

    public QSPreviewInfo(string qid) => Qid = qid;
}
