namespace Node.UI.Pages;

public class CaptchaWindow : GuiRequestWindow
{
    public CaptchaWindow(string base64Image, Func<string, Task> onClick)
    {
        Width = 600;
        Height = 400;
        this.Bind(TitleProperty, "Input captcha:");

        var input = new TextBox()
        {
            FontSize = 20,
        };
        Content = new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("* Auto"),
            Children =
            {
                new Image()
                {
                    Source = new Bitmap(new MemoryStream(Convert.FromBase64String(base64Image))),
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
                                await onClick(input.Text?.Trim() ?? string.Empty);
                                Dispatcher.UIThread.Post(ForceClose);
                            },
                        }.WithColumn(1),
                    }
                }.WithRow(1),
            },
        };
    }
}
