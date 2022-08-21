namespace SoftwareRegistry;

public class SoftList
{
    readonly Settings.DatabaseValue<ImmutableArray<SoftwareDefinition>> SoftwareBindable = new(nameof(Software), ImmutableArray<SoftwareDefinition>.Empty);
    public ImmutableArray<SoftwareDefinition> Software => SoftwareBindable.Value;

    public void Add(SoftwareDefinition soft) => SoftwareBindable.Value = Software.Add(soft);
    public void Replace(SoftwareDefinition oldv, SoftwareDefinition newv) => SoftwareBindable.Value = Software.Replace(oldv, newv);
    public void Remove(SoftwareDefinition soft) => SoftwareBindable.Value = Software.Remove(soft);
}