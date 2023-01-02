using Newtonsoft.Json;

namespace Common.Tasks.Info;

public class VeeeVectorizeInfo
{
    [JsonProperty("lod")]
    [ArrayRanged(min: 1), Ranged(1, 10_000)]
    public int[] Lods = null!;
}
