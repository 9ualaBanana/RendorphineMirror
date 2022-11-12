namespace NodeUI.Pages;

public class CaptchaWindow : Window
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
                                await onClick(input.Text.Trim());
                                Dispatcher.UIThread.Post(Close);
                            },
                        }.WithColumn(1),
                    }
                }.WithRow(1),
            },
        };
    }
}
