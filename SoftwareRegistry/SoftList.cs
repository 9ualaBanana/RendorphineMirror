namespace SoftwareRegistry;

public class SoftList
{
    readonly Database Database = new Database(Path.Combine(Directories.Data, "config.db"));
    readonly DatabaseValueKeyDictionary<string, SoftwareDefinition> SoftwareDictBindable;
    public ImmutableDictionary<string, SoftwareDefinition> Software => SoftwareDictBindable.Values.ToImmutableDictionary();

    readonly ILogger Logger;

    public SoftList(ILogger<SoftList> logger)
    {
        Logger = logger;
        SoftwareDictBindable = new(Database, nameof(Software), StringComparer.OrdinalIgnoreCase);

        var soft = new DatabaseValue<ImmutableDictionary<string, SoftwareDefinition>>(Database, nameof(Software), ImmutableDictionary<string, SoftwareDefinition>.Empty);
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
        var bkppath = Path.Combine(Directories.Data, "bkp");
        Directory.CreateDirectory(bkppath);

        File.Copy(Database.DbPath, Path.Combine(bkppath, DateTimeOffset.Now.ToUnixTimeSeconds().ToString() + ".db"), true);
    }

    public void Set(IEnumerable<KeyValuePair<string, SoftwareDefinition>> soft)
    {
        Backup();

        SoftwareDictBindable.Clear();
        SoftwareDictBindable.AddRange(soft);
        Logger.LogInformation($"Settings values {string.Join("; ", soft)}");
    }
    public void Add(string type, SoftwareDefinition soft)
    {
        Backup();
        SoftwareDictBindable.Add(type, soft);
        Logger.LogInformation($"Adding {type} {soft}");
    }
    public void Replace(string type, SoftwareDefinition newv, string? newtype = null)
    {
        Backup();

        SoftwareDictBindable.Remove(type);
        SoftwareDictBindable.Add(newtype ?? type, newv);
        Logger.LogInformation($"Replacing {type} with {newtype} {newv}");
    }
    public void Remove(string type)
    {
        Backup();
        SoftwareDictBindable.Remove(type);
        Logger.LogInformation($"Removing {type}");
    }
}