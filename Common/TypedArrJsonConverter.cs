using System.Collections;

namespace Common;

public class TypedArrJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => true;

    public static object? Read(JToken jtoken, JsonSerializer serializer)
    {
        JArray jarr;
        IList result;

        if (jtoken is JObject) jarr = (JArray) jtoken["$value$"].ThrowIfNull();
        else if (jtoken is JArray jarrr) jarr = jarrr;
        else throw new Exception("Unknown input type");

        result = (IList) new JArray().ToObject(Type.GetType(jtoken["$type$"].ThrowIfNull().Value<string>().ThrowIfNull()).ThrowIfNull()).ThrowIfNull();

        foreach (var token in jarr)
        {
            if (token is JValue { Value: null })
            {
                result.Add(null!);
                continue;
            }

            var jobj = (JObject) token;

            var typename = jobj.Property("$type$").ThrowIfNull().Value.Value<string>().ThrowIfNull();
            var type = Type.GetType(typename)!;

            var value = jobj.Property("value")?.Value.ToObject(type, serializer);
            result.Add(value!);
        }

        return result;
    }
    public static void Write(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            serializer.Serialize(writer, null);
            return;
        }


        var jarr = JArray.FromObject(value, serializer);
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


        var jobj = new JObject()
        {
            ["$type$"] = value.GetType().AssemblyQualifiedName,
            ["$value$"] = jarr,
        };

        jobj.WriteTo(writer, serializer.Converters.ToArray());
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        return Read(JToken.Load(reader), serializer);
    }
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => Write(writer, value, serializer);
}
