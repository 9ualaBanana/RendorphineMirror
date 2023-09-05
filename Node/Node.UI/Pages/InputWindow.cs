namespace Node.UI.Pages;

public class InputWindow : GuiRequestWindow
{
    public InputWindow(string text, Func<string, Task> onClick)
    {
        Width = 600;
        Height = 400;
        this.Bind(TitleProperty, "Input");

        var input = new TextBox()
        {
            FontSize = 20,
        };
        Content = new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("* Auto"),
            Children =
            {
                new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = text
                }.WithRow(0),
                new Grid()
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("* Auto"),
                    Children =
                    {
                        input.WithColumn(0),
                        new MPButton()
                        {
                            Text = "Enter",
                            FontSize = 20,
                            OnClick = async () =>
                            {
                                await onClick(input.Text.Trim());
                                Dispatcher.UIThread.Post(ForceClose);
                            },
                        }.WithColumn(1),
                    }
                }.WithRow(1),
            },
        };
    }
}
