using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Common;

public static class JsonSettings
{
    public static readonly JsonSerializer Default = JsonSerializer.CreateDefault();

    public static readonly JsonSerializerSettings LowercaseIgnoreNull = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = LowercaseContract.Instance,
        Formatting = Formatting.None,
    };
    public static readonly JsonSerializer LowercaseIgnoreNullS = JsonSerializer.Create(LowercaseIgnoreNull);

    public static readonly JsonSerializerSettings Lowercase = new()
    {
        ContractResolver = LowercaseContract.Instance,
        Formatting = Formatting.None,
    };
    public static readonly JsonSerializer LowercaseS = JsonSerializer.Create(Lowercase);

    public static readonly JsonSerializerSettings Typed = new() { TypeNameHandling = TypeNameHandling.Auto };
    public static readonly JsonSerializer TypedS = JsonSerializer.Create(Typed);


    class LowercaseContract : DefaultContractResolver
    {
        public static readonly LowercaseContract Instance = new();

        private LowercaseContract() => NamingStrategy = new LowercaseNamingStragedy();


        class LowercaseNamingStragedy : NamingStrategy
        {
            protected override string ResolvePropertyName(string name) => name.ToLowerInvariant();
        }
    }
}
