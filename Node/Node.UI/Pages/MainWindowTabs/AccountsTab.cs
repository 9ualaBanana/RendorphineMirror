using System.Net;

namespace Node.UI.Pages.MainWindowTabs;

public class AccountsTab : Panel
{
    public AccountsTab(NodeGlobalState state)
    {
        var baselist = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new CredentialsStatusPanel("MPlus", state.MPlusUsername, state.MPlusPassword)
                    .Named("MPlus"),
                new CredentialsStatusPanel("TurboSquid", state.TurboSquidUsername, state.TurboSquidPassword)
                    .Named("TurboSquid"),
                new CredentialsStatusPanel("CGTrader", state.CGTraderUsername, state.CGTraderPassword)
                    .Named("CGTrader"),
            },
        };
        Children.Add(baselist);
    }


    class CredentialsStatusPanel : Panel
    {
        public CredentialsStatusPanel(string target, IReadOnlyBindable<string?> username, IReadOnlyBindable<string?> password)
        {
            var credinput = new CredentialsInput(username, password);

            Children.Add(new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    credinput,
                    new MPButton()
                    {
                        Text = "Save",
                        OnClickSelf = async self =>
                        {
                            var creds = credinput.TryGet();

                            var result =
                                creds is null
                                ? await LocalApi.Default.Post("unsetcreds", $"Unsetting {target} creds", ("target", target))
                                : await LocalApi.Default.Post("setcreds", $"Updating {target} creds", ("target", target), ("creds", JsonConvert.SerializeObject(creds)));

                            await self.FlashErrorIfErr(result);
                        },
                    },
                },
            });
        }
    }
    class CredentialsInput : Panel
    {
        readonly TextBox Username, Password;

        public CredentialsInput(IReadOnlyBindable<string?> username, IReadOnlyBindable<string?> password)
        {
            Username = new() { Watermark = "Username" };
            Password = new() { Watermark = "Password", PasswordChar = '*' };
            username.SubscribeChanged(() => Username.Text = username.Value, true);
            password.SubscribeChanged(() => Password.Text = password.Value, true);

            Children.Add(new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto Auto"),
                Children =
                {
                    Username.WithRow(0),
                    Password.WithRow(1),
                },
            });
        }

        public NetworkCredential? TryGet()
        {
            Username.BorderBrush = Password.BorderBrush = null;

            if (string.IsNullOrEmpty(Username.Text))
                Username.BorderBrush = Brushes.Red;
            if (string.IsNullOrEmpty(Password.Text))
                Password.BorderBrush = Brushes.Red;

            if (string.IsNullOrEmpty(Username.Text) || string.IsNullOrEmpty(Password.Text))
                return null;

            return new NetworkCredential(Username.Text, Password.Text);
        }
    }
}
