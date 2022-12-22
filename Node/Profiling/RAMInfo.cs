namespace Node.Profiling;

public record RAMInfo(ulong Total)
{
    public ulong Free { get; set; }
}