using System.Collections;

namespace Common;

public class TypedArrJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => true;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var jarr = JArray.Load(reader);

        var result = new List<object>();
        foreach (var token in jarr)
        {
            if (token is JValue { Value: null })
            {
                result.Add(null!);
                continue;
            }

            var jobj = (JObject) token;

            var typename = (jobj.Property("$type$")?.Value.Value<string>()).ThrowIfNull();
            var type = Type.GetType(typename)!;

            var value = jobj.Property("value")?.Value.ToObject(type, serializer);
            result.Add(value!);
        }

        return result;
    }
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            serializer.Serialize(writer, null);
            return;
        }


        var jarr = JToken.FromObject(value, serializer);
        var index = 0;
        foreach (var obj in (IEnumerable) value)
        {
            if (obj is null)
                jarr[index] = JValue.CreateNull();
            else
            {
                jarr[index] = new JObject()
                {
                    ["$type$"] = obj.GetType().AssemblyQualifiedName,
                    ["value"] = JToken.FromObject(obj),
                };
            }

            index++;
        }

        jarr.WriteTo(writer, serializer.Converters.ToArray());
    }
}
