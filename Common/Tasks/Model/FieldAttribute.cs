namespace Common.Tasks.Model;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public abstract class DescriberAttributeBase : Attribute { }

public class RangedAttribute : DescriberAttributeBase
{
    public readonly double Min, Max;

    public RangedAttribute(double min, double max)
    {
        Min = min;
        Max = max;
    }
}
public class DefaultAttribute : DescriberAttributeBase
{
    public readonly object Value;

    public DefaultAttribute(object value) => Value = value;
}

public class DescriberIgnoreAttribute : DescriberAttributeBase { }

public class LocalFileAttribute : DescriberAttributeBase { }
public class LocalDirectoryAttribute : DescriberAttributeBase { }

public class MPlusDirectoryAttribute : DescriberAttributeBase { }

public class ArrayRangedAttribute : DescriberAttributeBase
{
    public readonly int Min, Max;

    public ArrayRangedAttribute(int min = 0, int max = int.MaxValue)
    {
        Min = min;
        Max = max;
    }
}

public class ArrayItemAttribute : DescriberAttributeBase
{
    public readonly Attribute[] Attributes;

    public ArrayItemAttribute(Attribute[] attributes)
    {
        Attributes = attributes;
    }
}