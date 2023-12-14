using System.Collections;

namespace Common;

public class TypedArrJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => true;

    static JsonSerializer SetTypeNameHandling(JsonSerializer serializer)
    {
        var ret = new JsonSerializer()
        {
            TypeNameHandling = TypeNameHandling.All,

            TypeNameAssemblyFormatHandling = serializer.TypeNameAssemblyFormatHandling,
            PreserveReferencesHandling = serializer.PreserveReferencesHandling,
            ReferenceLoopHandling = serializer.ReferenceLoopHandling,
            MissingMemberHandling = serializer.MissingMemberHandling,
            ObjectCreationHandling = serializer.ObjectCreationHandling,
            NullValueHandling = serializer.NullValueHandling,
            DefaultValueHandling = serializer.DefaultValueHandling,
            ConstructorHandling = serializer.ConstructorHandling,
            MetadataPropertyHandling = serializer.MetadataPropertyHandling,
            // Converters = { serializer.Converters },
            ContractResolver = serializer.ContractResolver,
            TraceWriter = serializer.TraceWriter,
            EqualityComparer = serializer.EqualityComparer,
            SerializationBinder = serializer.SerializationBinder,
            Context = serializer.Context,
            ReferenceResolver = serializer.ReferenceResolver,

            Formatting = serializer.Formatting,
            DateFormatHandling = serializer.DateFormatHandling,
            DateTimeZoneHandling = serializer.DateTimeZoneHandling,
            DateParseHandling = serializer.DateParseHandling,
            FloatFormatHandling = serializer.FloatFormatHandling,
            FloatParseHandling = serializer.FloatParseHandling,
            StringEscapeHandling = serializer.StringEscapeHandling,
            Culture = serializer.Culture,
            MaxDepth = serializer.MaxDepth,
            CheckAdditionalContent = serializer.CheckAdditionalContent,
            DateFormatString = serializer.DateFormatString,
        };

        foreach (var converter in serializer.Converters)
            ret.Converters.Add(converter);

        return ret;
    }

    public static object? Read(JToken jtoken, JsonSerializer serializer)
    {
        try
        {
            return jtoken.ToObject(null, SetTypeNameHandling(serializer));
        }
        catch { }

        JArray jarr;
        var result = new List<object>() as IList;

        if (jtoken is JObject) jarr = (JArray) jtoken["$value$"].ThrowIfNull();
        else if (jtoken is JArray jarrr) jarr = jarrr;
        else throw new Exception("Unknown input type");

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

        return JToken.FromObject(result).ToObject(Type.GetType(jtoken["$type$"].ThrowIfNull().Value<string>().ThrowIfNull()).ThrowIfNull()).ThrowIfNull();
    }
    public static void Write(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        SetTypeNameHandling(serializer).Serialize(writer, value);
        return;

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
