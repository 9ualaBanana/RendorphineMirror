namespace SoftwareRegistry;

public class SoftList
{
    readonly Settings.DatabaseValue<ImmutableDictionary<string, SoftwareDefinition>> SoftwareBindable = new(nameof(Software), ImmutableDictionary<string, SoftwareDefinition>.Empty);
    public ImmutableDictionary<string, SoftwareDefinition> Software => SoftwareBindable.Value;

    public SoftList() => SoftwareBindable.Value = SoftwareBindable.Value.WithComparers(StringComparer.OrdinalIgnoreCase);

    public void Add(string type, SoftwareDefinition soft) => SoftwareBindable.Value = Software.Add(type, soft);
    public void Replace(string type, SoftwareDefinition newv, string? newtype = null) => SoftwareBindable.Value = Software.Remove(type).SetItem(newtype ?? type, newv);
    public void Remove(string type) => SoftwareBindable.Value = Software.Remove(type);
}