using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon.Tasks;

public class TaskOutputJsonConverter : JsonConverter<ITaskOutputInfo>
{
    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, ITaskOutputInfo? value, JsonSerializer serializer) => throw new NotImplementedException();
    public override ITaskOutputInfo? ReadJson(JsonReader reader, Type objectType, ITaskOutputInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (hasExistingValue) throw new NotImplementedException();
        return TaskModels.DeserializeOutput(JObject.Load(reader));
    }
}