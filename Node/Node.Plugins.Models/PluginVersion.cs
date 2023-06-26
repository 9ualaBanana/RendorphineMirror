using System.Diagnostics.CodeAnalysis;

namespace Node.Plugins.Models;

[JsonConverter(typeof(JsonSerializer))]
public readonly struct PluginVersion : IEquatable<PluginVersion>, IComparable<PluginVersion>
{
    public static PluginVersion Empty => new(string.Empty);

    [MemberNotNullWhen(false, nameof(Version))]
    public bool IsEmpty => string.IsNullOrEmpty(Version);

    readonly string Version;

    public PluginVersion(string? version) => Version = version ?? string.Empty;


    public static bool operator ==(PluginVersion left, PluginVersion right) => left.Equals(right);
    public static bool operator !=(PluginVersion left, PluginVersion right) => !(left == right);
    public static bool operator <(PluginVersion left, PluginVersion right) => left.CompareTo(right) < 0;
    public static bool operator <=(PluginVersion left, PluginVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >(PluginVersion left, PluginVersion right) => left.CompareTo(right) > 0;
    public static bool operator >=(PluginVersion left, PluginVersion right) => left.CompareTo(right) >= 0;

    public int CompareTo(PluginVersion other)
    {
        if (System.Version.TryParse(Version, out var thisver) && System.Version.TryParse(other.Version, out var otherver))
            return thisver.CompareTo(otherver);

        return string.CompareOrdinal(Version, other.Version);
    }

    public bool Equals(PluginVersion other) => (other.IsEmpty && IsEmpty) || (other.Version == Version);
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is PluginVersion other && Equals(other);
    public override int GetHashCode() => IsEmpty ? 1 : Version.GetHashCode();

    public override string ToString() => Version;

    public static implicit operator PluginVersion(string? version) => new(version);


    public class JsonSerializer : JsonConverter<PluginVersion>
    {
        public override PluginVersion ReadJson(JsonReader reader, Type objectType, PluginVersion existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer) =>
            new(reader.ReadAsString());

        public override void WriteJson(JsonWriter writer, PluginVersion value, Newtonsoft.Json.JsonSerializer serializer) =>
            writer.WriteValue(value.Version);
    }
}
