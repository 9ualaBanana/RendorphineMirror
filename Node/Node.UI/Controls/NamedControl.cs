namespace Node.UI.Controls;

public static class NamedControlExtensions
{
    public static NamedControl Named(this Control control, LocalizedString title) => NamedControl.Create(title, control);
}
public class NamedControl : Panel
{
    public readonly TextBlock Title;
    public readonly Panel Control;

    public NamedControl(LocalizedString title)
    {
        Title = new TextBlock()
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16,
        }.Bind(title);

        Children.Add(new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("40 Auto"),
            Children =
            {
                new Border()
                {
                    Background = ColorsNew.BackgroundLight2,
                    Padding = new Thickness(20, 10),
                    Child = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children = { Title },
                    },
                }.WithRow(0),
                new Border()
                {
                    Background = ColorsNew.BackgroundLight,
                    Padding = new Thickness(20),
                    Child = Control = new(),
                }.WithRow(1),
            },
        });
    }


    public static NamedControl Create(LocalizedString title, Control child) => new(title) { Control = { Children = { child } } };
}