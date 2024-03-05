namespace Node.UI.Pages;

public class RetryOrSkipWindow : GuiRequestWindow
{
    public RetryOrSkipWindow(string text, Func<bool, Task> onClick)
    {
        Width = 600;
        Height = 400;
        this.Bind(TitleProperty, "Retry?");

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
                        new MPButton()
                        {
                            Text = "Retry",
                            FontSize = 20,
                            OnClick = async () =>
                            {
                                await onClick(true);
                                Dispatcher.UIThread.Post(ForceClose);
                            },
                        }.WithColumn(0),
                        new MPButton()
                        {
                            Text = "Skip",
                            FontSize = 20,
                            OnClick = async () =>
                            {
                                await onClick(false);
                                Dispatcher.UIThread.Post(ForceClose);
                            },
                        }.WithColumn(1),
                    }
                }.WithRow(1),
            },
        };
    }
}
