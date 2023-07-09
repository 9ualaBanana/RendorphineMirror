namespace Node.Tasks.Models.ExecInfo;

public class VeeeVectorizeInfo
{
    [JsonProperty("lod")]
    [ArrayRanged(min: 1), Ranged(1, 10_000)]
    public required ImmutableArray<int> Lod { get; init; }

    [SetsRequiredMembers]
    public VeeeVectorizeInfo(int[] lod) => Lod = lod.ToImmutableArray();
}
