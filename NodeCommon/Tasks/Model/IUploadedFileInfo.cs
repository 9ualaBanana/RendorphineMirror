using Newtonsoft.Json.Serialization;
namespace NodeCommon.Tasks.Model;

[JsonConverter(typeof(UploadedFileInfoJConverter))]
public interface IUploadedFileInfo
{
    TaskOutputType Type { get; }
}



public class UploadedFileInfoJConverter : JsonConverter<IUploadedFileInfo>
{
    readonly JsonSerializer DeeeeefaultSerializer = new() { ContractResolver = new Resolver() };
    public sealed override bool CanWrite => false;

    protected IUploadedFileInfo Deserialize(JObject input, JsonSerializer serializer)
    {
        var types = new Dictionary<TaskOutputType, Type>()
        {
            [TaskOutputType.MPlus] = typeof(MPlusUploadedFileInfo),
        };

        var type = input.GetValue("type", StringComparison.OrdinalIgnoreCase)?.ToObject<TaskOutputType>() ?? TaskOutputType.MPlus;
        return (IUploadedFileInfo) input.ToObject(types[type], serializer ?? JsonSettings.Default)!;
    }


    public sealed override void WriteJson(JsonWriter writer, IUploadedFileInfo? value, JsonSerializer serializer) => throw new NotImplementedException();
    public sealed override IUploadedFileInfo? ReadJson(JsonReader reader, Type objectType, IUploadedFileInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (hasExistingValue) throw new NotImplementedException();
        return Deserialize(JObject.Load(reader), DeeeeefaultSerializer).ThrowIfNull();
    }


    // to get out of the deserialization loop
    class Resolver : DefaultContractResolver
    {
        protected sealed override JsonConverter? ResolveContractConverter(Type objectType)
        {
            if (objectType.IsAssignableTo(typeof(IUploadedFileInfo))) return null;
            return base.ResolveContractConverter(objectType);
        }
    }
}