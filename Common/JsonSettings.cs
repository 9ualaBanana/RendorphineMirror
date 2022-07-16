using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Common;

public class JsonSettings
{
    public static JsonSerializerSettings LowercaseIgnoreNull = new()
    {
        DefaultValueHandling = DefaultValueHandling.Ignore,
        ContractResolver = LowercaseContract.Instance,
        Formatting = Formatting.None,
    };


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
