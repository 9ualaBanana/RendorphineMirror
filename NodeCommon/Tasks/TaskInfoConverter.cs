using Newtonsoft.Json.Serialization;

namespace NodeCommon.Tasks;

public class EmptyJConverter<T> : JsonConverter<T>
{
    public sealed override bool CanWrite => false;
    public sealed override bool CanRead => false;

    public sealed override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer) => throw new NotImplementedException();
    public sealed override T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
}
public abstract class TaskInfoJConverterBase<T> : JsonConverter<T>
{
    readonly JsonSerializer DeeeeefaultSerializer = new() { ContractResolver = new Resolver() };
    public sealed override bool CanWrite => false;

    protected abstract T Deserialize(JObject input, JsonSerializer serializer);

    public sealed override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer) => throw new NotImplementedException();
    public sealed override T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (hasExistingValue) throw new NotImplementedException();
        return Deserialize(JObject.Load(reader), DeeeeefaultSerializer);
    }


    // to get out of the deserialization loop
    class Resolver : DefaultContractResolver
    {
        protected sealed override JsonConverter? ResolveContractConverter(Type objectType)
        {
            if (objectType.IsAssignableTo(typeof(T))) return null;
            return base.ResolveContractConverter(objectType);
        }
    }
}

public class TaskInputJConverter : TaskInfoJConverterBase<ITaskInputInfo>
{
    protected override ITaskInputInfo Deserialize(JObject input, JsonSerializer serializer) => TaskModels.DeserializeInput(input, serializer);
}
public class TaskOutputJConverter : TaskInfoJConverterBase<ITaskOutputInfo>
{
    protected override ITaskOutputInfo Deserialize(JObject input, JsonSerializer serializer) => TaskModels.DeserializeOutput(input, serializer);
}
public class WatchingTaskInputJConverter : TaskInfoJConverterBase<IWatchingTaskInputInfo>
{
    protected override IWatchingTaskInputInfo Deserialize(JObject input, JsonSerializer serializer) => TaskModels.DeserializeWatchingInput(input, serializer);
}
public class WatchingTaskOutputJConverter : TaskInfoJConverterBase<IWatchingTaskOutputInfo>
{
    protected override IWatchingTaskOutputInfo Deserialize(JObject input, JsonSerializer serializer) => TaskModels.DeserializeWatchingOutput(input, serializer);
}