namespace Node.Tasks.Models.ExecInfo;

[JsonConverter(typeof(StringEnumConverter))]
public enum TopazOperation
{
    Upscale,
    Slowmo,
}

public class TopazInfo
{
    public required TopazOperation Operation { get; init; }
    public int? X { get; init; }
}
