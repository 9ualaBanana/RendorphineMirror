using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace UpdaterCommon;

public readonly record struct UpdaterFileInfo(
    [property: JsonPropertyName("p"), JsonProperty("p")] string Path,
    [property: JsonPropertyName("m"), JsonProperty("m")] long ModificationTime,
    [property: JsonPropertyName("s"), JsonProperty("s")] long Size,
    [property: JsonPropertyName("h"), JsonProperty("h")] ulong Hash
);