using System.Reflection;
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

    public static readonly JsonSerializerSettings LowercaseIgnoreNullTaskInOut = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = TaskInputOutputSerializationContract.Instance,
        Formatting = Formatting.None,
    };
    public static readonly JsonSerializer LowercaseIgnoreNullTaskInOutS = JsonSerializer.Create(LowercaseIgnoreNullTaskInOut);

    public static readonly JsonSerializerSettings Lowercase = new()
    {
        ContractResolver = LowercaseContract.Instance,
        Formatting = Formatting.None,
    };
    public static readonly JsonSerializer LowercaseS = JsonSerializer.Create(Lowercase);


    class TaskInputOutputSerializationContract : DefaultContractResolver
    {
        public static readonly TaskInputOutputSerializationContract Instance = new();

        private TaskInputOutputSerializationContract() => NamingStrategy = new LowercaseNamingStragedy();

        protected override List<MemberInfo> GetSerializableMembers(Type objectType) =>
            base.GetSerializableMembers(objectType).Where(x => x.GetCustomAttribute<NonSerializableForTasksAttribute>() is null).ToList();


        class LowercaseNamingStragedy : NamingStrategy
        {
            protected override string ResolvePropertyName(string name) => name.ToLowerInvariant();
        }
    }
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
