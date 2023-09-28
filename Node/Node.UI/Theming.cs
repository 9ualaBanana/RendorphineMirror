namespace Node.UI;

public static class Theming
{
    public static void AddStyle<T>(this T styles, params (string key, object? resource)[] resources) where T : Control => styles.Styles.AddStyle(x => x.Is<T>(), resources);

    public static void AddStyle(this Styles styles, Func<Selector?, Selector> selector, params (AvaloniaProperty property, object setter)[] setters) => AddStyle(styles, selector, setters.Select(x => new Setter(x.property, x.setter)).ToArray());
    public static void AddStyle<T>(this Styles styles, params (AvaloniaProperty property, object setter)[] setters) where T : StyledElement => AddStyle<T>(styles, x => x.Is<T>(), setters);
    public static void AddStyle<T>(this Styles styles, Func<Selector?, Selector> selector, params (AvaloniaProperty property, object setter)[] setters) where T : StyledElement =>
        AddStyle(styles, x => selector(x.Is<T>()), setters.Select(x => new Setter(x.property, x.setter)).ToArray());
    public static void AddStyle<T>(this Styles styles, params SetterBase[] setters) where T : StyledElement => AddStyle(styles, x => x.Is<T>(), setters);
    public static void AddStyle(this Styles styles, Func<Selector?, Selector> selector, params SetterBase[] setters)
    {
        var style = new Style(selector);
        foreach (var setter in setters) style.Setters.Add(setter);

        styles.Add(style);
    }

    public static void AddStyle<T>(this Styles styles, Func<Selector?, Selector> selector, params (string key, object? resource)[] resources) where T : StyledElement =>
        AddStyle(styles, x => selector(x.Is<T>()), resources);
    public static void AddStyle<T>(this Styles styles, params (string key, object? resource)[] resources) where T : StyledElement => AddStyle(styles, x => x.Is<T>(), resources);
    public static void AddStyle(this Styles styles, Func<Selector?, Selector> selector, params (string key, object? resource)[] resources)
    {
        var style = new Style(selector);
        foreach (var (key, res) in resources) style.Resources[key] = res;

        styles.Add(style);
    }

    public static IEnumerable<T> FindChildren<T>(Control control) where T : Control
    {
        static IEnumerable<Control> Find(Control control, IEnumerable<Control> values) =>
            values
            .Prepend(control)
            .Concat(
                control switch
                {
                    ContentControl cc when cc.Content is Control ch => Find(ch, values),
                    Decorator { Child: not null } d => Find(d.Child, values),
                    Popup pp when pp.Child is { } => Find(pp.Child, values),
                    Panel p => p.Children.SelectMany(x => Find(x, values)),

                    _ => Enumerable.Empty<Control>(),
                });


        return Find(control, Enumerable.Empty<Control>()).OfType<T>();
    }
}
