using System.Net;
using _3DProductsPublish.Turbosquid;
using _3DProductsPublish.Turbosquid.Upload;

namespace Node.UI.Pages.MainWindowTabs;

public class TurboSquidSalesReportTab : Panel
{
    public TurboSquidSalesReportTab()
    {
        var mplogintb = new TextBox()
        {
            Watermark = "Login",
        };
        var mppasswordtb = new TextBox()
        {
            Watermark = "Password",
        };
        var turbologintb = new TextBox()
        {
            Watermark = "Login",
        };
        var turbopasswordtb = new TextBox()
        {
            Watermark = "Password",
        };

        var content = new ScrollViewer()
        {
            Content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            mplogintb,
                            mppasswordtb,
                        },
                    }.Named("M+ credentials"),
                    new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            turbologintb,
                            turbopasswordtb,
                        },
                    }.Named("TurboSquid credentials"),
                    new MPButton()
                    {
                        Text = "Fetch sales",
                        OnClickSelf = async self =>
                        {
                            var mpcred = new NetworkCredential(mplogintb.Text, mppasswordtb.Text);
                            var turbocred = new NetworkCredential(turbologintb.Text, turbopasswordtb.Text);

                            var result = await LocalApi.Default.Post("fetchturbosquidsales", "Fetching turbosquid sales", ("mpcreds", JsonConvert.SerializeObject(mpcred)), ("turbocreds", JsonConvert.SerializeObject(turbocred)));
                            await self.Flash(result);
                        },
                    },
                },
            },
        };

        Children.Add(content);
    }
}
