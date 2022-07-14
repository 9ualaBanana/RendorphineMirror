using static Common.Settings;

namespace NodeUI;

public static class UISettings
{
    public static string? Language { get => BLanguage.Value; set => BLanguage.Value = value; }
    public static bool ShortcutsCreated { get => BShortcutsCreated.Value; set => BShortcutsCreated.Value = value; }

    public static readonly DatabaseBindable<string?> BLanguage;
    public static readonly DatabaseBindable<bool> BShortcutsCreated;

    static UISettings()
    {
        BLanguage = new(nameof(Language), null);
        BShortcutsCreated = new(nameof(ShortcutsCreated), false);
    }
}
