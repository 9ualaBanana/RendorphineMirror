namespace Node.Tasks.Models.ExecInfo;

public class EditVideoInfo : MediaEditInfo
{
    [JsonProperty("spd")]
    public FFMpegSpeed? Speed;

    [JsonProperty("startFrame")]
    [Default(0d)]
    public double? StartFrame;

    [JsonProperty("endFrame")]
    public double? EndFrame;

    [JsonProperty("cutframeat")]
    public double? CutFrameAt;

    [JsonProperty("cutframesat")]
    public double[]? CutFramesAt;
}