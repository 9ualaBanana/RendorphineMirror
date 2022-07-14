using System.Reflection;

namespace Common.NodeToUI;

public abstract class FieldDescriber
{
    public readonly string Name;
    public readonly bool Nullable;

    public FieldDescriber(string name, bool nullable)
    {
        Name = name.ToLowerInvariant();
        Nullable = nullable;
    }


    public static FieldDescriber Create(Type type) => Create(type, type.Name, false);
    public static FieldDescriber Create(PropInfo field) => Create(field.FieldType, field.Name, field.IsNullable());

    static FieldDescriber Create(Type type, string name, bool nullable)
    {
        if (istype<bool>()) return new BooleanDescriber(name, nullable);
        if (istype<string>()) return new StringDescriber(name, nullable);

        if (type.GetInterfaces().Any(x => x.Name.StartsWith("INumber", StringComparison.Ordinal)))
            return new NumberDescriber(name, nullable) { IsInteger = !type.GetInterfaces().Any(x => x.Name.StartsWith("IFloatingPoint", StringComparison.Ordinal)) };

        if (type.IsClass)
            return new ObjectDescriber(name, nullable, PropInfo.CreateFromChildren(type).Select(Create).ToImmutableArray());

        throw new InvalidOperationException($"Could not find Describer for type {type}");


        bool istype<T>() => type == typeof(T);
    }


    public readonly struct PropInfo
    {
        public Type FieldType => System.Nullable.GetUnderlyingType(_FieldType) ?? _FieldType;
        Type _FieldType => Property?.PropertyType ?? Field?.FieldType!;

        public string Name => Property?.Name ?? Field?.Name!;

        readonly PropertyInfo? Property;
        readonly FieldInfo? Field;

        public PropInfo(FieldInfo field)
        {
            Field = field;
            Property = null;
        }
        public PropInfo(PropertyInfo property)
        {
            Property = property;
            Field = null;
        }

        public void SetValue(object? obj, object? value)
        {
            if (obj is null) return;

            Property?.SetValue(obj, value);
            Field?.SetValue(obj, value);
        }
        public object? GetValue(object? obj) => obj is null ? null : Property?.GetValue(obj) ?? Field?.GetValue(obj)!;

        public T? GetAttribute<T>() where T : Attribute => Property?.GetCustomAttribute<T>() ?? Field?.GetCustomAttribute<T>();
        public bool IsNullable() =>
            Property is not null ? new NullabilityInfoContext().Create(Property).WriteState is NullabilityState.Nullable
            : Field is not null ? new NullabilityInfoContext().Create(Field).WriteState is NullabilityState.Nullable
            : false;

        public static IEnumerable<PropInfo> CreateFromChildren(Type type) =>
            type.GetMembers()
            .Where(x => x is FieldInfo || (x is PropertyInfo p && p.SetMethod is not null))
            .Select(x => (PropInfo) x!);

        public static implicit operator PropInfo?(MemberInfo member) =>
            member is PropertyInfo p ? new(p)
            : member is FieldInfo f ? new(f)
            : throw new InvalidOperationException();
    }
}

public class BooleanDescriber : FieldDescriber
{
    public BooleanDescriber(string name, bool nullable) : base(name, nullable) { }
}
public class StringDescriber : FieldDescriber
{
    public StringDescriber(string name, bool nullable) : base(name, nullable) { }
}
public class NumberDescriber : FieldDescriber
{
    public bool IsInteger { get; init; }

    public NumberDescriber(string name, bool nullable) : base(name, nullable) { }
}
public class ObjectDescriber : FieldDescriber
{
    public readonly ImmutableArray<FieldDescriber> Fields;

    public ObjectDescriber(string name, bool nullable, ImmutableArray<FieldDescriber> fields) : base(name, nullable) =>
        Fields = fields;
}
