namespace Node.UI;

public class UISettings
{
    public string? Language { get => BLanguage.Value; set => BLanguage.Value = value; }
    public bool ShortcutsCreated { get => BShortcutsCreated.Value; set => BShortcutsCreated.Value = value; }

    public readonly DatabaseValue<string?> BLanguage;
    public readonly DatabaseValue<bool> BShortcutsCreated;
    public readonly DatabaseValue<SavedWindowState> MainWindowState;

    public UISettings(DataDirs dirs)
    {
        var db = new Database(Path.Combine(dirs.Data, "ui.db"));

        BLanguage = new(db, nameof(Language), null);
        BShortcutsCreated = new(db, $"{nameof(ShortcutsCreated)}_2", false);
        MainWindowState = new(db, $"{nameof(MainWindowState)}_2", new());
    }


    public record SavedWindowState(bool Visible = true, PixelPoint? Position = null);
}
