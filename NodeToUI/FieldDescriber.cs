using System.Reflection;
using Newtonsoft.Json;

namespace NodeToUI;

public abstract class FieldDescriber
{
    public readonly string Name;
    public readonly string JsonTypeName;
    public readonly bool Nullable;
    public object? DefaultValue { get; init; }
    public ImmutableArray<Attribute> Attributes { get; init; }

    public FieldDescriber(string name, string jsonTypeName, bool nullable, ImmutableArray<Attribute> attributes)
    {
        Name = name.ToLowerInvariant();
        JsonTypeName = jsonTypeName;
        Nullable = nullable;
        Attributes = attributes;
    }


    public static FieldDescriber Create(Type type) => Create(type, ImmutableArray<Attribute>.Empty);
    public static FieldDescriber Create(Type type, ImmutableArray<Attribute> attributes) => Create(type, type.Name, false, null, attributes);
    public static FieldDescriber Create(PropInfo field, Type parent) => Create(field.FieldType, field.Name, field.IsNullable(), field.GetAttribute<DefaultAttribute>()?.Value,
        field.GetAttributes<Attribute>().Where(x => x.GetType().Namespace?.StartsWith("System.") != true).ToImmutableArray());

    static FieldDescriber Create(Type type, string name, bool nullable, object? defaultValue, ImmutableArray<Attribute> attributes)
    {
        var jsonTypeName = (string)
            typeof(Newtonsoft.Json.Formatting).Assembly.GetType("Newtonsoft.Json.Utilities.ReflectionUtils", true)!
            .GetMethod("GetTypeName")!
            .Invoke(null, new object?[] { type, TypeNameAssemblyFormatHandling.Simple, null })!;

        if (istype<bool>()) return new BooleanDescriber(name, jsonTypeName, nullable, attributes) { DefaultValue = defaultValue };
        if (istype<string>()) return new StringDescriber(name, jsonTypeName, nullable, attributes) { DefaultValue = defaultValue };

        if (type.GetInterfaces().Any(x => x.Name.StartsWith("INumber", StringComparison.Ordinal)))
            return new NumberDescriber(name, jsonTypeName, nullable, attributes) { DefaultValue = defaultValue, IsInteger = !type.GetInterfaces().Any(x => x.Name.StartsWith("IFloatingPoint", StringComparison.Ordinal)) };

        if (assignableToGeneric(type, typeof(IReadOnlyDictionary<,>)))
            return DictionaryDescriber.Create(type, name, jsonTypeName, nullable, attributes, defaultValue);
        if (assignableToGeneric(type, typeof(IReadOnlyCollection<>)))
            return CollectionDescriber.Create(type, name, jsonTypeName, nullable, attributes, defaultValue);

        if (type.IsClass)
            return new ObjectDescriber(name, jsonTypeName, nullable, PropInfo.CreateFromChildren(type).Select(x => Create(x, type)).ToImmutableArray(), attributes) { DefaultValue = defaultValue };

        throw new InvalidOperationException($"Could not find Describer for type {type}");


        bool istype<T>() => type == typeof(T);
        static bool assignableToGeneric(Type type, Type genericType)
        {
            var interfaceTypes = type.GetInterfaces();

            foreach (var it in interfaceTypes)
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                return true;

            var baseType = type.BaseType;
            if (baseType is null) return false;

            return assignableToGeneric(baseType, genericType);
        }
    }


    public readonly struct PropInfo
    {
        public Type DeclaringType => System.Nullable.GetUnderlyingType(_DeclaringType) ?? _DeclaringType;
        Type _DeclaringType => Property?.DeclaringType ?? Field?.DeclaringType!;

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
        public IEnumerable<T> GetAttributes<T>() where T : Attribute => Property?.GetCustomAttributes<T>() ?? Field?.GetCustomAttributes<T>() ?? Enumerable.Empty<T>();
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
    public BooleanDescriber(string name, string jsonTypeName, bool nullable, ImmutableArray<Attribute> attributes) : base(name, jsonTypeName, nullable, attributes) { }
}
public class StringDescriber : FieldDescriber
{
    public StringDescriber(string name, string jsonTypeName, bool nullable, ImmutableArray<Attribute> attributes) : base(name, jsonTypeName, nullable, attributes) { }
}
public class NumberDescriber : FieldDescriber
{
    public bool IsInteger { get; init; }

    public NumberDescriber(string name, string jsonTypeName, bool nullable, ImmutableArray<Attribute> attributes) : base(name, jsonTypeName, nullable, attributes) { }
}
public class ObjectDescriber : FieldDescriber
{
    public readonly ImmutableArray<FieldDescriber> Fields;

    public ObjectDescriber(string name, string jsonTypeName, bool nullable, ImmutableArray<FieldDescriber> fields, ImmutableArray<Attribute> attributes) : base(name, jsonTypeName, nullable, attributes) =>
        Fields = fields;
}

public interface ICollectionDescriber
{
    Type ValueType { get; }
}
public class DictionaryDescriber : FieldDescriber, ICollectionDescriber
{
    public readonly Type KeyType;
    public Type ValueType { get; }

    private DictionaryDescriber(Type keyType, Type valueType, string name, string jsonTypeName, bool nullable, ImmutableArray<Attribute> attributes) : base(name, jsonTypeName, nullable, attributes)
    {
        KeyType = keyType;
        ValueType = valueType;
    }


    public static DictionaryDescriber Create(Type type, string name, string jsonTypeName, bool nullable, ImmutableArray<Attribute> attributes, object? defaultValue = null)
    {
        type = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
        return new(type.GetGenericArguments()[0], type.GetGenericArguments()[1], name, jsonTypeName, nullable, attributes) { DefaultValue = defaultValue };
    }
}
public class CollectionDescriber : FieldDescriber, ICollectionDescriber
{
    public Type ValueType { get; }

    [JsonConstructor]
    private CollectionDescriber(Type valueType, string name, string jsonTypeName, bool nullable, ImmutableArray<Attribute> attributes) : base(name, jsonTypeName, nullable, attributes) =>
        ValueType = valueType;


    public static CollectionDescriber Create(Type type, string name, string jsonTypeName, bool nullable, ImmutableArray<Attribute> attributes, object? defaultValue = null)
    {
        type = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));
        return new(type.GetGenericArguments()[0], name, jsonTypeName, nullable, attributes) { DefaultValue = defaultValue };
    }
}
