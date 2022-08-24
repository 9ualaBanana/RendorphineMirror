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
