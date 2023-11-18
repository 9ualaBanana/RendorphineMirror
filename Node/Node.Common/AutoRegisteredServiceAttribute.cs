namespace Node.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AutoRegisteredServiceAttribute : Attribute
{
    public bool SingleInstance { get; }

    public AutoRegisteredServiceAttribute(bool singleInstance) => SingleInstance = singleInstance;
}
