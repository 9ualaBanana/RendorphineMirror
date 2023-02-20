using System.Diagnostics.CodeAnalysis;

namespace SoftwareRegistry;

public class SoftList
{
    readonly DatabaseValueKeyDictionary<string, SoftwareDefinition> SoftwareDictBindable = new(Database.Instance, nameof(Software), StringComparer.OrdinalIgnoreCase);
    public ImmutableDictionary<string, SoftwareDefinition> Software => SoftwareDictBindable.Values.ToImmutableDictionary();

    public SoftList()
    {
        var soft = new DatabaseValue<ImmutableDictionary<string, SoftwareDefinition>>(Database.Instance, nameof(Software), ImmutableDictionary<string, SoftwareDefinition>.Empty);
        var softv = soft.Value.WithComparers(StringComparer.OrdinalIgnoreCase);
        if (softv.Count != 0)
        {
            SoftwareDictBindable.AddRange(softv);
            soft.Value = softv.Clear();
        }
    }

    public bool TryGetValue(string name, [NotNullWhen(true)] out SoftwareDefinition? soft)
    {
        if (SoftwareDictBindable.TryGetValue(name, out var kvp))
        {
            soft = kvp.Value;
            return true;
        }

        soft = null;
        return false;
    }

    void Backup()
    {
        var bkppath = Path.Combine(Init.ConfigDirectory, "bkp");
        Directory.CreateDirectory(bkppath);

        File.Copy(Database.Instance.DbPath, Path.Combine(bkppath, DateTimeOffset.Now.ToUnixTimeSeconds().ToString() + ".db"), true);
    }

    public void Set(IEnumerable<KeyValuePair<string, SoftwareDefinition>> soft)
    {
        Backup();

        SoftwareDictBindable.Clear();
        SoftwareDictBindable.AddRange(soft);
    }
    public void Add(string type, SoftwareDefinition soft)
    {
        Backup();
        SoftwareDictBindable.Add(type, soft);
    }
    public void Replace(string type, SoftwareDefinition newv, string? newtype = null)
    {
        Backup();

        SoftwareDictBindable.Remove(type);
        SoftwareDictBindable.Add(newtype ?? type, newv);
    }
    public void Remove(string type)
    {
        Backup();
        SoftwareDictBindable.Remove(type);
    }
}