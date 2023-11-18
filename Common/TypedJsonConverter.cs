namespace Common;

public class TypedJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => true;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var jobj = JObject.Load(reader);
        var typename = jobj.Property("$type$")?.Value.Value<string>();
        if (typename is null) return jobj.ToObject(objectType, serializer);

        var type = Type.GetType(typename)!;
        return jobj.ToObject(type, serializer);
    }
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        var jobj = JToken.FromObject(value, serializer);
        jobj["$type$"] = value.GetType().AssemblyQualifiedName;

        jobj.WriteTo(writer, serializer.Converters.ToArray());
    }
}
