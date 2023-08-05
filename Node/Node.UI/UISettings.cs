namespace Node.UI;

public static class UISettings
{
    public static string? Language { get => BLanguage.Value; set => BLanguage.Value = value; }
    public static bool ShortcutsCreated { get => BShortcutsCreated.Value; set => BShortcutsCreated.Value = value; }

    public static readonly DatabaseValue<string?> BLanguage;
    public static readonly DatabaseValue<bool> BShortcutsCreated;

    static UISettings()
    {
        var db = new Database(Path.Combine(Directories.Data, "ui.db"));

        BLanguage = new(db, nameof(Language), null);
        BShortcutsCreated = new(db, $"{nameof(ShortcutsCreated)}_2", false);
    }
}
