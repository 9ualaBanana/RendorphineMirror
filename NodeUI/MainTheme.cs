namespace NodeUI
{
    public static class MainTheme
    {
        public static void Apply(IResourceDictionary _, Styles styles)
        {
            AddStyle<IStyleable>(styles,
                ("HighlightForegroundBrush", Colors.White),                                         // selection fg
                ("HighlightBrush", Colors.Accent),                                                  // selection bg
                ("ThemeForegroundBrush", Colors.Black),                                             // foreground
                ("ThemeControlHighlightMidBrush", Colors.SelectedMenuItemBackground),               // item: pointerover
                ("ThemeAccentBrush4", Colors.SelectedMenuItemBackground),                           // item: focused
                ("ThemeAccentBrush3", Colors.SelectedMenuItemBackground.Lighten(10)),               // item: selected
                ("ThemeAccentBrush2", Colors.SelectedMenuItemBackground.Lighten(20)));              // item: focused selected

            AddStyle<ComboBoxItem>(styles,
                (ComboBoxItem.BackgroundProperty, Colors.MenuItemBackground));

            // vertical ScrollBar buttons
            foreach (var part in new[] { "PART_LineUpButton", "PART_LineDownButton" })
                AddStyle<RepeatButton>(styles, x => x.Name(part),
                    (RepeatButton.IsVisibleProperty, false));

            AddStyle<ScrollViewer>(styles,
                ("ThemeControlMidBrush", Colors.Transparent));
        }

        public static void AddStyle(Styles styles, Func<Selector?, Selector> selector, params (AvaloniaProperty property, object setter)[] setters) => AddStyle(styles, selector, setters.Select(x => new Setter(x.property, x.setter)).ToArray());
        public static void AddStyle<T>(Styles styles, params (AvaloniaProperty property, object setter)[] setters) where T : IStyleable => AddStyle<T>(styles, x => x.Is<T>(), setters);
        public static void AddStyle<T>(Styles styles, Func<Selector?, Selector> selector, params (AvaloniaProperty property, object setter)[] setters) where T : IStyleable =>
            AddStyle(styles, x => selector(x.Is<T>()), setters.Select(x => new Setter(x.property, x.setter)).ToArray());
        public static void AddStyle<T>(Styles styles, params ISetter[] setters) where T : IStyleable => AddStyle(styles, x => x.Is<T>(), setters);
        public static void AddStyle(Styles styles, Func<Selector?, Selector> selector, params ISetter[] setters)
        {
            var style = new Style(selector);
            foreach (var setter in setters) style.Setters.Add(setter);

            styles.Add(style);
        }

        public static void AddStyle<T>(Styles styles, Func<Selector?, Selector> selector, params (string key, object? resource)[] resources) where T : IStyleable =>
            AddStyle(styles, x => selector(x.Is<T>()), resources);
        public static void AddStyle<T>(Styles styles, params (string key, object? resource)[] resources) where T : IStyleable => AddStyle(styles, x => x.Is<T>(), resources);
        public static void AddStyle(Styles styles, Func<Selector?, Selector> selector, params (string key, object? resource)[] resources)
        {
            var style = new Style(selector);
            foreach (var (key, res) in resources) style.Resources[key] = res;

            styles.Add(style);
        }

        public static IEnumerable<T> FindChildren<T>(Control control) where T : IControl
        {
            static IEnumerable<IControl> Find(IControl control, IEnumerable<IControl> values) =>
                values
                .Prepend(control)
                .Concat(
                    control switch
                    {
                        IContentControl cc when cc.Content is IControl ch => Find(ch, values),
                        Decorator d => Find(d.Child, values),
                        Popup pp when pp.Child is { } => Find(pp.Child, values),
                        IPanel p => p.Children.SelectMany(x => Find(x, values)),

                        _ => Enumerable.Empty<IControl>(),
                    });


            return Find(control, Enumerable.Empty<IControl>()).OfType<T>();
        }
    }
}