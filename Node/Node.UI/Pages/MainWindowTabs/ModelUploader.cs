using Avalonia.Controls.Templates;

namespace Node.UI.Pages.MainWindowTabs;

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
                        NamedControl.Create("Title", new TextBox()
                        {
                            AcceptsReturn = false,
                        }.Bound(TextBox.TextProperty, Title)),
                        NamedControl.Create("Files", new Border()
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
                        NamedControl.Create("Product images", new Border()
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
                        NamedControl.Create("Description", new TextBox()
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
}