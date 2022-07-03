using Avalonia.Controls.ApplicationLifetimes;

namespace NodeUI.Pages
{
    public class LoginWindow : LoginWindowUI
    {
        public LoginWindow(LocalizedString error) : this() => Login.ShowError(error);
        public LoginWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FixStartupLocation();
            MinWidth = MaxWidth = Width = 692;
            MinHeight = MaxHeight = Height = 410;
            CanResize = false;
            Title = App.AppName;
            Icon = App.Icon;

            this.PreventClosing();


            async ValueTask<OperationResult> authenticate(string login, string password, bool slave)
            {
                var authres = await auth(login, password, slave);
                if (!authres) Dispatcher.UIThread.Post(() => Login.ShowError(authres.AsString()));

                return authres;
            }
            async ValueTask<OperationResult> auth(string login, string password, bool slave)
            {
                if (string.IsNullOrWhiteSpace(login)) return OperationResult.Err(Localized.Login.EmptyLogin);
                if (!slave && string.IsNullOrEmpty(password)) return OperationResult.Err(Localized.Login.EmptyPassword);

                Login.StartLoginAnimation(Localized.Login.Loading);
                using var _ = new FuncDispose(() => Dispatcher.UIThread.Post(Login.StopLoginAnimation));

                OperationResult auth;

                if (slave) auth = await SessionManager.AutoAuthAsync(login).ConfigureAwait(false);
                else auth = await SessionManager.AuthAsync(login, password).ConfigureAwait(false);

                if (auth)
                {
                    try { await LocalApi.Send("reloadcfg").ConfigureAwait(false); }
                    catch { }

                    Dispatcher.UIThread.Post(ShowMainWindow);
                }

                return auth;
            }
            Login.OnPressLogin += (login, password, slave) => _ = authenticate(login, password, slave);
            Login.OnPressForgotPassword += () => Process.Start(new ProcessStartInfo("https://accounts.stocksubmitter.com/resetpasswordrequest") { UseShellExecute = true });
        }

        void ShowMainWindow()
        {
            Settings.Reload();

            var w = new MainWindow();
            ((IClassicDesktopStyleApplicationLifetime) Application.Current!.ApplicationLifetime!).MainWindow = w;
            w.Show();
            if (Environment.GetCommandLineArgs().Contains("hidden"))
                w.Hide();

            Close();
        }
    }
    public class LoginWindowUI : Window
    {
        protected readonly LoginControl Login;

        public LoginWindowUI()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(.45, GridUnitType.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(.55, GridUnitType.Star));

            grid.Children.Add(new Border() { Child = new HelloImage(), BoxShadow = new BoxShadows(new BoxShadow { Blur = 14, Color = new Color(64, 0, 0, 0) }) });
            grid.Children.Add(Login = new LoginControl());
            Grid.SetColumn(Login, 1);

            Content = grid;
        }


        class HelloImage : UserControl
        {
            public HelloImage()
            {
                var image = new Image
                {
                    Stretch = Stretch.Fill,
                    Source = new Bitmap(Resource.LoadStream(this, "img.login_image.jpg"))
                };

                var text = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 20,
                    Foreground = Colors.From(38, 59, 71),
                    MaxWidth = 200,
                    Margin = new Thickness(111, 111, 0, 206)
                };
                text.Bind(TextBlock.TextProperty, Localized.Login.Welcome);


                var grid = new Panel();
                grid.Children.Add(image);
                grid.Children.Add(text);

                Content = grid;
            }
        }
        protected class LoginControl : UserControl
        {
            public event Action<string, string, bool> OnPressLogin = delegate { };
            public event Action OnPressForgotPassword = delegate { };

            public TextBox LoginInput => LoginPasswordInput.LoginInput;
            public TextBox PasswordInput => LoginPasswordInput.PasswordInput;

            readonly LoginPasswordInputUI LoginPasswordInput;
            readonly TextBlock ErrorText;
            readonly LoginStatusUI LoginStatus;
            readonly MPButton LoginButton;

            public LoginControl()
            {
                ErrorText = new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Colors.ErrorText,
                    FontSize = 14
                };

                var slavecheckbox = new CheckBox()
                {
                    IsChecked = false,
                };

                var buttonsAndRemember = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Margin = new Thickness(30, 0),
                    Children =
                    {
                        new TextBlock()
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Text = "slave????",
                        },
                        slavecheckbox,
                    }
                };


                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition(40, GridUnitType.Star));
                grid.RowDefinitions.Add(new RowDefinition(100 - grid.RowDefinitions.Sum(x => x.Height.Value), GridUnitType.Star));
                grid.RowDefinitions.Add(new RowDefinition(210 - grid.RowDefinitions.Sum(x => x.Height.Value), GridUnitType.Star));
                grid.RowDefinitions.Add(new RowDefinition(230 - grid.RowDefinitions.Sum(x => x.Height.Value), GridUnitType.Star));
                grid.RowDefinitions.Add(new RowDefinition(260 - grid.RowDefinitions.Sum(x => x.Height.Value), GridUnitType.Star));
                grid.RowDefinitions.Add(new RowDefinition(290 - grid.RowDefinitions.Sum(x => x.Height.Value), GridUnitType.Star));
                grid.RowDefinitions.Add(new RowDefinition(330 - grid.RowDefinitions.Sum(x => x.Height.Value), GridUnitType.Star));
                grid.RowDefinitions.Add(new RowDefinition(410 - grid.RowDefinitions.Sum(x => x.Height.Value), GridUnitType.Star));

                grid.Children.Add(new LoginTopBarUI());
                grid.Children.Add(LoginPasswordInput = new LoginPasswordInputUI());
                grid.Children.Add(ErrorText);
                grid.Children.Add(LoginStatus = new LoginStatusUI());
                grid.Children.Add(buttonsAndRemember);
                grid.Children.Add(LoginButton = new MPButton());

                var forgotPasswordButton = new ForgotPasswordButtonUI();
                grid.Children.Add(forgotPasswordButton);

                Grid.SetRow(ErrorText, 1);
                Grid.SetRow(LoginStatus, 1);
                Grid.SetRow(LoginPasswordInput, 2);
                Grid.SetRow(buttonsAndRemember, 4);
                Grid.SetRow(LoginButton, 6);
                Grid.SetRow(forgotPasswordButton, 7);


                HideError();

                LoginStatus.HorizontalAlignment = HorizontalAlignment.Center;
                LoginStatus.VerticalAlignment = VerticalAlignment.Center;

                LoginPasswordInput.Margin = new Thickness(30, 0);
                forgotPasswordButton.Margin = new Thickness(0, 0, 0, 50);

                LoginButton.Width = 157;
                LoginButton.Height = 38;
                LoginButton.Text = Localized.Login.Button;
                LoginButton.FontWeight = (FontWeight) 700;
                LoginButton.MaxWidth = forgotPasswordButton.MaxWidth = 157;
                LoginButton.Background = Colors.Accent;
                LoginButton.HoverBackground = Colors.DarkDarkGray;

                {
                    PasswordInput.Transitions ??= new();
                    PasswordInput.Transitions.Add(new ThicknessTransition() { Property = Control.MarginProperty, Duration = TimeSpan.FromSeconds(1) });
                    slavecheckbox.Subscribe(CheckBox.IsCheckedProperty, c => PasswordInput.Margin = c != true ? new Thickness(0, 0, 0, 0) : new Thickness(0, -100, 0, 0));
                }

                LoginButton.OnClick += () => OnPressLogin(LoginInput.Text, PasswordInput.Text, slavecheckbox.IsChecked == true);
                forgotPasswordButton.OnClick += () => OnPressForgotPassword();

                Content = grid;

                KeyDown += (_, e) =>
                {
                    if (e.Key != Key.Enter) return;
                    if (!LoginInput.IsFocused && !PasswordInput.IsFocused) return;

                    OnPressLogin(LoginInput.Text, PasswordInput.Text, slavecheckbox.IsChecked == true);
                };
            }

            public void LockButtons() => LoginButton.IsEnabled = false;
            public void UnlockButtons() => LoginButton.IsEnabled = true;
            public void StartLoginAnimation(LocalizedString text)
            {
                HideError();
                LockButtons();

                LoginStatus.IsVisible = true;
                LoginStatus.Text = text;
            }
            public void StopLoginAnimation()
            {
                UnlockButtons();
                LoginStatus.IsVisible = false;
            }
            public void ShowError(LocalizedString text) => ShowError(text.ToString());
            public void ShowError(string? text)
            {
                StopLoginAnimation();

                ErrorText.IsVisible = true;
                ErrorText.Text = text;
            }
            public void HideError()
            {
                ErrorText.IsVisible = false;
            }


            class LoginTopBarUI : UserControl
            {
                public LoginTopBarUI()
                {
                    var text = new TextBlock
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = Colors.WhiteText,
                        FontSize = 17,
                        FontWeight = (FontWeight) 600
                    };

                    text.Bind(TextBlock.TextProperty, Localized.Login.Title);
                    Background = Colors.DarkGray;
                    Content = text;
                }
            }
            class LoginStatusUI : UserControl
            {
                public LocalizedString Text { set => TextBlock.Text = value.ToString(); }

                readonly TextBlock TextBlock;

                public LoginStatusUI()
                {
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition(25, GridUnitType.Pixel));
                    grid.ColumnDefinitions.Add(new ColumnDefinition());

                    grid.Children.Add(new LoadCircle());
                    grid.Children.Add(TextBlock = new TextBlock());

                    Grid.SetColumn(TextBlock, 1);

                    Content = grid;

                    TextBlock.Bind(TextBlock.TextProperty, Localized.Login.Loading);
                    TextBlock.VerticalAlignment = VerticalAlignment.Center;
                    TextBlock.Margin = new Thickness(10, 0);
                    TextBlock.FontSize = 16;
                    TextBlock.Foreground = Colors.DarkText;

                    IsVisible = false;
                }
            }
            class LoginPasswordInputUI : UserControl
            {
                public readonly TextBox LoginInput;

                public TextBox PasswordInput => EyeTextBox.TextBox;
                readonly EyeTextBoxUI EyeTextBox;

                public LoginPasswordInputUI()
                {
                    LoginInput = new TextBox();
                    EyeTextBox = new EyeTextBoxUI();

                    LoginInput.FontSize = PasswordInput.FontSize = 16;
                    LoginInput.VerticalContentAlignment = PasswordInput.VerticalContentAlignment = VerticalAlignment.Center;
                    LoginInput.Foreground = PasswordInput.Foreground = Colors.DarkText;
                    LoginInput.BorderThickness = PasswordInput.BorderThickness = new Thickness(0);
                    LoginInput.Background = PasswordInput.Background = Colors.Transparent;
                    LoginInput.Padding = PasswordInput.Padding = new Thickness(20, 0, 0, 0);
                    LoginInput.Cursor = PasswordInput.Cursor = new Cursor(StandardCursorType.Ibeam);

                    LoginInput.Bind(TextBox.WatermarkProperty, Localized.Login.Email);

                    var line = new Panel { Background = Colors.BorderColor };

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition(2, GridUnitType.Pixel));
                    grid.RowDefinitions.Add(new RowDefinition());

                    grid.Children.Add(LoginInput);
                    grid.Children.Add(line);
                    grid.Children.Add(EyeTextBox);

                    Grid.SetRow(line, 1);
                    Grid.SetRow(EyeTextBox, 2);

                    var border = new Border
                    {
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(4),
                        BorderBrush = Colors.BorderColor,
                        Child = grid
                    };

                    Content = border;
                }


                class EyeTextBoxUI : UserControl
                {
                    public readonly TextBox TextBox;

                    public EyeTextBoxUI()
                    {
                        TextBox = new TextBox() { PasswordChar = '*' };
                        TextBox.Bind(TextBox.WatermarkProperty, Localized.Login.Password);

                        var eye = new EyeUI();
                        eye.OnToggle += t => TextBox.PasswordChar = t ? default : '*';

                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition());
                        grid.ColumnDefinitions.Add(new ColumnDefinition(56, GridUnitType.Pixel));

                        grid.Children.Add(TextBox);
                        grid.Children.Add(eye);

                        Grid.SetColumnSpan(TextBox, 2);
                        Grid.SetColumn(eye, 1);

                        Content = grid;
                    }


                    class EyeUI : ClickableSwitchControl
                    {
                        public EyeUI()
                        {
                            const string sourceOpen = "img.eye.svg";
                            const string sourceClosed = "img.eye_slash.svg";

                            var img = new SvgImage()
                            {
                                Width = 18,
                                Height = 14,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Source = sourceClosed,
                            };

                            Content = img;
                            OnToggle += t => img.Source = t ? sourceOpen : sourceClosed;
                        }
                    }
                }
            }

            class RememberMeSwitchUI : UserControl
            {
                public bool IsToggled
                {
                    get => CheckBox.IsToggled;
                    set => CheckBox.IsToggled = value;
                }

                readonly SwitchUI CheckBox;

                public RememberMeSwitchUI()
                {
                    CheckBox = new SwitchUI();
                    CheckBox.Width = CheckBox.Height = 13;
                    CheckBox.VerticalAlignment = VerticalAlignment.Center;

                    var text = new TextBlock() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), };
                    text.Bind(TextBlock.TextProperty, Localized.Login.RememberMe);

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition());

                    grid.Children.Add(CheckBox);
                    grid.Children.Add(text);

                    Grid.SetColumn(text, 1);

                    Content = grid;
                }


                class SwitchUI : ClickableControl
                {
                    bool _IsToggled;
                    public bool IsToggled
                    {
                        get => _IsToggled;
                        set
                        {
                            _IsToggled = value;
                            UpdateColor();
                        }
                    }

                    readonly Border BackgroundBorder;

                    public SwitchUI()
                    {
                        BackgroundBorder = new Border();
                        BackgroundBorder.Background = BackgroundBorder.BorderBrush = Colors.GrayButton;
                        BackgroundBorder.CornerRadius = new CornerRadius(3);

                        Content = BackgroundBorder;

                        OnClick += () => IsToggled = !IsToggled;
                    }

                    void UpdateColor() =>
                        BackgroundBorder.Background = BackgroundBorder.BorderBrush = IsToggled ? Colors.Accent : Colors.GrayButton;
                }
            }
            class ForgotPasswordButtonUI : ClickableControl
            {
                public ForgotPasswordButtonUI()
                {
                    var text = new TextBlock()
                    {
                        Foreground = Colors.DarkText,
                        FontSize = 14,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    }.Binded(TextBlock.TextProperty, Localized.Login.ForgotPassword);

                    Content = text;

                    PointerEnter += (_, _) => text.Foreground = Colors.Accent;
                    PointerLeave += (_, _) => text.Foreground = Colors.DarkText;
                }
            }
        }
    }
}