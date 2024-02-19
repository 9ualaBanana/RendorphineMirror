namespace Node.UI.Pages.MainWindowTabs;

public class TurboSquidSalesReportTab : Panel
{
    public TurboSquidSalesReportTab()
    {
        var content = new ScrollViewer()
        {
            Content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new MPButton()
                    {
                        Text = "Fetch sales",
                        OnClickSelf = async self =>
                        {
                            var result = await LocalApi.Default.Post("fetchturbosquidsales", "Fetching turbosquid sales");
                            await self.Flash(result);
                        },
                    },
                },
            },
        };

        Children.Add(content);
    }
}
