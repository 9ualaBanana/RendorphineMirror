namespace Node.Tasks.Models.ExecInfo;

public class EditVideoInfo : MediaEditInfo
{
    [JsonProperty("spd")]
    public FFMpegSpeed? Speed;

    [JsonProperty("cutfromframe")]
    [Default(0d)]
    public double? CutFromFrame;

    [JsonProperty("cuttoframe")]
    public double? CutToFrame;

    [JsonProperty("cutframeat")]
    public double? CutFrameAt;

    [JsonProperty("cutframesat")]
    public double[]? CutFramesAt;
}