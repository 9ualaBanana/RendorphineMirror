using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeToUI;

public abstract class FieldDescriber
{
    public string Name { get; }
    public string JsonTypeName { get; }
    public ImmutableArray<Attribute> Attributes { get; init; } = ImmutableArray<Attribute>.Empty;
    public bool Nullable { get; init; } = false;
    public JToken? DefaultValue { get; init; }

    [JsonConstructor]
    protected FieldDescriber(string name, string jsonTypeName)
    {
        Name = name.ToLowerInvariant();
        JsonTypeName = jsonTypeName;
    }
    protected FieldDescriber(PropInfo field)
    {
        Name = field.Name.ToLowerInvariant();
        JsonTypeName = GetJsonTypeName(field.FieldType);
        Nullable = field.IsNullable();
        Attributes = field.GetAttributes<Attribute>().Where(x => x.GetType().Namespace?.StartsWith("System.") != true).ToImmutableArray();

        var def = field.GetAttribute<DefaultAttribute>()?.Value;
        if (def is not null) def = JToken.FromObject(def);
    }


    public static FieldDescriber Create(Type type, ImmutableArray<Attribute> attributes) => Create(new PropInfo(type, attributes));
    public static FieldDescriber Create(PropInfo prop)
    {
        var type = prop.FieldType;

        if (istype<bool>()) return new BooleanDescriber(prop);
        if (istype<string>()) return new StringDescriber(prop);
        if (type.IsEnum) return new EnumDescriber(prop);

        if (type.GetInterfaces().Any(x => x.Name.StartsWith("INumber", StringComparison.Ordinal)))
            return new NumberDescriber(prop);

        if (assignableToGeneric(type, typeof(IReadOnlyDictionary<,>)))
            return new DictionaryDescriber(prop);
        if (assignableToGeneric(type, typeof(IReadOnlyCollection<>)))
            return new CollectionDescriber(prop);

        if (type.IsClass || type.IsValueType)
            return new ObjectDescriber(prop);

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

    static string GetJsonTypeName(Type type) =>
        (string) typeof(Newtonsoft.Json.Formatting).Assembly.GetType("Newtonsoft.Json.Utilities.ReflectionUtils", true)!
        .GetMethod("GetTypeName")!.Invoke(null, new object?[] { type, TypeNameAssemblyFormatHandling.Simple, null })!;


    public readonly struct PropInfo
    {
        public Type FieldType => System.Nullable.GetUnderlyingType(_FieldType) ?? _FieldType;
        Type _FieldType => Type ?? Property?.PropertyType ?? Field!.FieldType;

        public string Name => Type?.Name ?? Property?.Name ?? Field!.Name;

        readonly PropertyInfo? Property;
        readonly FieldInfo? Field;
        readonly Type? Type;
        readonly ImmutableArray<Attribute>? Attributes;

        public PropInfo(FieldInfo field)
        {
            Field = field;
            Property = null;
            Type = null;
            Attributes = default;
        }
        public PropInfo(PropertyInfo property)
        {
            Property = property;
            Field = null;
            Type = null;
            Attributes = default;
        }
        public PropInfo(Type type, ImmutableArray<Attribute>? attributes = default)
        {
            Field = null;
            Property = null;
            Type = type;
            Attributes = attributes;
        }

        public T? GetAttribute<T>() where T : Attribute => Attributes?.OfType<T>().FirstOrDefault() ?? Property?.GetCustomAttribute<T>() ?? Field?.GetCustomAttribute<T>() ?? Type?.GetCustomAttribute<T>();
        public IEnumerable<T> GetAttributes<T>() where T : Attribute => Attributes?.OfType<T>() ?? Property?.GetCustomAttributes<T>() ?? Field?.GetCustomAttributes<T>() ?? Type?.GetCustomAttributes<T>() ?? Enumerable.Empty<T>();
        public bool IsNullable() =>
            Property is not null ? new NullabilityInfoContext().Create(Property).WriteState is NullabilityState.Nullable
            : Field is not null ? new NullabilityInfoContext().Create(Field).WriteState is NullabilityState.Nullable
            : Type is not null ? FieldType != Type
            : false;

        public static IEnumerable<PropInfo> CreateFromChildren(Type type) =>
            type.GetMembers()
            .Where(x => x is (System.Type or FieldInfo)
                || (x is PropertyInfo prop
                    && (prop.SetMethod is not null || type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(x => x.GetCustomAttribute<JsonConstructorAttribute>() is null)
                        .Any(x => x.GetParameters()
                            .Any(x => string.Equals(x.Name, prop.Name, StringComparison.OrdinalIgnoreCase))))))
            .Select(x => (PropInfo) x);

        public static implicit operator PropInfo(MemberInfo member) =>
            member is PropertyInfo p ? new(p)
            : member is FieldInfo f ? new(f)
            : member is Type t ? new(t)
            : throw new InvalidOperationException();
    }
}

public class BooleanDescriber : FieldDescriber
{
    [JsonConstructor]
    public BooleanDescriber(string name, string jsonTypeName) : base(name, jsonTypeName) { }
    public BooleanDescriber(PropInfo prop) : base(prop) { }
}
public class StringDescriber : FieldDescriber
{
    [JsonConstructor]
    public StringDescriber(string name, string jsonTypeName) : base(name, jsonTypeName) { }
    public StringDescriber(PropInfo prop) : base(prop) { }
}
public class NumberDescriber : FieldDescriber
{
    public bool IsInteger { get; init; }

    [JsonConstructor]
    public NumberDescriber(string name, string jsonTypeName) : base(name, jsonTypeName) { }
    public NumberDescriber(PropInfo prop) : base(prop) => IsInteger = !prop.FieldType.GetInterfaces().Any(x => x.Name.StartsWith("IFloatingPoint", StringComparison.Ordinal));
}
public class ObjectDescriber : FieldDescriber
{
    public ImmutableArray<FieldDescriber> Fields { get; init; }

    [JsonConstructor]
    public ObjectDescriber(string name, string jsonTypeName) : base(name, jsonTypeName) { }
    public ObjectDescriber(PropInfo prop) : base(prop) => Fields = PropInfo.CreateFromChildren(prop.FieldType).Select(Create).ToImmutableArray();
}
public class EnumDescriber : FieldDescriber
{
    [JsonConstructor]
    public EnumDescriber(string name, string jsonTypeName) : base(name, jsonTypeName) { }
    public EnumDescriber(PropInfo prop) : base(prop) { }
}

public interface ICollectionDescriber
{
    Type ValueType { get; }
}
public class DictionaryDescriber : FieldDescriber, ICollectionDescriber
{
    public Type KeyType { get; }
    public Type ValueType { get; }
    public JToken? DefaultKeyValue { get; init; }
    public JToken? DefaultValueValue { get; init; }

    [JsonConstructor]
    private DictionaryDescriber(Type keyType, Type valueType, string name, string jsonTypeName) : base(name, jsonTypeName)
    {
        KeyType = keyType;
        ValueType = valueType;
    }
    public DictionaryDescriber(PropInfo prop) : base(prop)
    {
        var type = prop.FieldType;
        var interfacetype = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

        KeyType = interfacetype.GetGenericArguments()[0];
        ValueType = interfacetype.GetGenericArguments()[1];
    }
}
public class CollectionDescriber : FieldDescriber, ICollectionDescriber
{
    public Type ValueType { get; }
    public JToken? DefaultValueValue { get; init; }

    [JsonConstructor]
    private CollectionDescriber(Type valueType, string name, string jsonTypeName) : base(name, jsonTypeName) =>
        ValueType = valueType;

    public CollectionDescriber(PropInfo prop) : base(prop)
    {
        var type = prop.FieldType;
        var interfacetype = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));

        ValueType = interfacetype.GetGenericArguments()[0];
    }
}
