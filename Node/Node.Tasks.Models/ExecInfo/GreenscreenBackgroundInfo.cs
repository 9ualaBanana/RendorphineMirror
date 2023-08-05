namespace Node.Tasks.Models.ExecInfo;

public record GreenscreenBackgroundColor(byte R, byte G, byte B);
public class GreenscreenBackgroundInfo
{
    [JsonProperty("color")]
    public GreenscreenBackgroundColor? Color;
}