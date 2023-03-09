using Avalonia.Controls.Templates;

namespace NodeUI.Pages.MainWindowTabs;

public class ModelUploader : Panel
{
    public readonly Bindable<string> Title = new("");
    public readonly Bindable<string> Description = new("");
    public readonly BindableList<string> Images = new();

    public ModelUploader()
    {
        Children.Add(new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("8* 2*"),
            Children =
            {
                new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        WithTitle("Title", new TextBox()
                        {
                            AcceptsReturn = false,
                        }.Bound(TextBox.TextProperty, Title)),
                        WithTitle("Files", new Border()
                        {
                            Background = ColorsNew.BackgroundContent,
                            CornerRadius = new CornerRadius(2),
                            BorderThickness = new Thickness(1),
                            BorderBrush = ColorsNew.Foreground,
                            Padding = new Thickness(20),

                            Child = new StackPanel()
                            {
                                Orientation = Orientation.Vertical,
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Children =
                                {
                                    new TextBlock()
                                    {
                                        Foreground = ColorsNew.ForegroundLight,
                                        FontSize = 16,
                                    }.Centered().Bind("Add your files"),
                                    new MPButton()
                                    {
                                        HoverBackground = Brushes.Aqua,
                                        OnClickSelf = async self => await self.FlashError("ban"),
                                    }.Centered().Bind("Upload your files"),
                                    new TextBlock()
                                    {

                                    }.Centered().Bind("Upload or drag and drop images, videos, Zip files & more"),
                                },
                            },
                        }),
                        WithTitle("Product images", new Border()
                        {
                            Background = ColorsNew.BackgroundLight2,
                            Height = 100,
                            Child = new ScrollViewer()
                            {
                                Padding = new Thickness(10),
                                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                Content = new TypedItemsControl<string>(Images, path => new Image() { Source = new Bitmap(path) })
                                {
                                    ItemsPanel = new FuncTemplate<IPanel>(() => new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Spacing = 10,
                                    }),
                                },
                            },
                        }),
                        WithTitle("Description", new TextBox()
                        {
                            MinHeight = 40,
                            AcceptsReturn = true,
                        }.Bound(TextBox.TextProperty, Description)),
                    },
                }.WithColumn(0),

                new StackPanel()
                {
                    Orientation = Orientation.Vertical,

                }.WithColumn(1),
            }
        });
    }


    static Control WithTitle(LocalizedString title, Control child) => WithTitle(child, title, out _);
    static Control WithTitle(LocalizedString title, Control child, out StackPanel titleBase) => WithTitle(child, title, out titleBase);
    static Control WithTitle(Control child, LocalizedString title) => WithTitle(child, title, out _);
    static Control WithTitle(Control child, LocalizedString title, out StackPanel titleBase)
    {
        var titleText = new TextBlock()
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16,
        }.Bind(title);

        titleBase = new StackPanel()
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                titleText,
            },
        };

        return new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("40 Auto"),
            Children =
            {
                new Border()
                {
                    Background = ColorsNew.BackgroundLight2,
                    Padding = new Thickness(20, 10),
                    Child = titleBase,
                }.WithRow(0),
                new Border()
                {
                    Background = ColorsNew.BackgroundLight,
                    Padding = new Thickness(20),
                    Child = child,
                }.WithRow(1),
            },
        };
    }
}
