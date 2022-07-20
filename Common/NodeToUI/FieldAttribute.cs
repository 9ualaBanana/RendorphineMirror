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
