namespace Node.UI;

public static class MainTheme
{
    public static void Apply(IResourceDictionary _, Styles styles)
    {
        styles.AddStyle<StyledElement>(
            ("ThemeBackgroundBrush", ColorsNew.Background),
            ("ThemeForegroundBrush", ColorsNew.Foreground),                                     // foreground
            ("HighlightForegroundBrush", Colors.White),                                         // selection fg
            ("HighlightBrush", Colors.Accent),                                                  // selection bg
            ("ThemeControlHighlightMidBrush", ColorsNew.BackgroundLight),                       // item: hovered
            ("ThemeAccentBrush4", Colors.From(255, 16)),                                        // item: selected focused; secondary bg
            ("ThemeAccentBrush3", ColorsNew.BackgroundLight.Lighten(10)),                       // item: selected hovered
            ("ThemeAccentBrush2", ColorsNew.BackgroundLight2.Lighten(20))                       // item: selected focused hovered
        );

        styles.AddStyle<ComboBoxItem>((ComboBoxItem.BackgroundProperty, Colors.MenuItemBackground));

        // vertical ScrollBar buttons
        foreach (var part in new[] { "PART_LineUpButton", "PART_LineDownButton" })
            styles.AddStyle<RepeatButton>(x => x.Name(part), (RepeatButton.IsVisibleProperty, false));

        styles.AddStyle<ScrollViewer>(("ThemeControlMidBrush", Colors.Transparent));
    }
}
