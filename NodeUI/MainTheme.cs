namespace NodeUI;

public static class MainTheme
{
    public static void Apply(IResourceDictionary _, Styles styles)
    {
        styles.AddStyle<IStyleable>(
            ("ThemeBackgroundBrush", ColorsNew.Background),
            ("ThemeForegroundBrush", ColorsNew.Foreground),                                     // foreground
            ("HighlightForegroundBrush", Colors.White),                                         // selection fg
            ("HighlightBrush", Colors.Accent),                                                  // selection bg
            ("ThemeControlHighlightMidBrush", Colors.SelectedMenuItemBackground),               // item: pointerover
            ("ThemeAccentBrush4", Colors.From(255, 16)),                                        // item: secondary bg
            ("ThemeAccentBrush3", Colors.SelectedMenuItemBackground.Lighten(10)),               // item: selected
            ("ThemeAccentBrush2", Colors.SelectedMenuItemBackground.Lighten(20))                // item: focused selected
        );

        styles.AddStyle<ComboBoxItem>((ComboBoxItem.BackgroundProperty, Colors.MenuItemBackground));

        // vertical ScrollBar buttons
        foreach (var part in new[] { "PART_LineUpButton", "PART_LineDownButton" })
            styles.AddStyle<RepeatButton>(x => x.Name(part), (RepeatButton.IsVisibleProperty, false));

        styles.AddStyle<ScrollViewer>(("ThemeControlMidBrush", Colors.Transparent));
    }
}