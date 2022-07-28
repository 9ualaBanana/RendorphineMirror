namespace Common.NodeToUI;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RangedAttribute : Attribute
{
    public readonly double Min, Max;

    public RangedAttribute(double min, double max)
    {
        Min = min;
        Max = max;
    }
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DefaultAttribute : Attribute
{
    public readonly object Value;

    public DefaultAttribute(object value) => Value = value;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LocalFileAttribute : Attribute { }
