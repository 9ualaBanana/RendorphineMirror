using Newtonsoft.Json.Serialization;
using System.Reflection;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public class ContractResolver : DefaultContractResolver
    {
        public static ContractResolver _ { get; } = new();
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.DeclaringType == typeof(AssetContainer))
                switch (property.PropertyName)
                {
                    case nameof(AssetContainer.Path):
                        property.PropertyName = "Container"; break;
                    case nameof(AssetContainer.ContainerType):
                        property.Ignored = true; break;
                };
            return property;
        }
    }
}
