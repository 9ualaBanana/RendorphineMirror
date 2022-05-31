namespace NodeUI.Pages
{
    public class LoginWindow : LoginWindowUI
    {
        static readonly Api Api = new Api();

        readonly List<CancellationTokenSource> WaitingExternalAuths = new List<CancellationTokenSource>();

        public LoginWindow(bool tryAutoAuth = true)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FixStartupLocation();
            MinWidth = MaxWidth = Width = 692;
            MinHeight = MaxHeight = Height = 410;
            CanResize = false;
            Title = App.AppName;
            Icon = App.Icon;

            async Task authenticate(Func<ValueTask<OperationResult<LoginResult>>> func)
            {
                var auth = await func();
                if (!auth)
                {
                    Login.ShowError(auth.Message);
                    return;
                }

                Login.StartLoginAnimation(Localized.Login.LoadingDirectories);
                ShowMainWindow(auth.Result);

                foreach (var t in WaitingExternalAuths)
                {
                    try { t.Cancel(); }
                    catch { }
                }
                WaitingExternalAuths.Clear();
                Login.StopLoginAnimation();
            }
            Login.OnPressLogin += (login, password) => _ = authenticate(() => TryAuthenticate(login, password));
            Login.OnPressLoginWith += t => _ = authenticate(() => TryAuthenticateExternal(t));
            Login.OnPressForgotPassword += () => Process.Start(new ProcessStartInfo("https://accounts.stocksubmitter.com/resetpasswordrequest") { UseShellExecute = true });

            var hasSavedCreds = !string.IsNullOrWhiteSpace(Settings.SessionId) && !string.IsNullOrWhiteSpace(Settings.UserId) && !string.IsNullOrWhiteSpace(Settings.Username);
            Login.IsRememberMeToggled = hasSavedCreds;

            if (tryAutoAuth && hasSavedCreds)
                Task.Run(async () =>
                {
                    var result = await Dispatcher.UIThread.InvokeAsync(() => CheckAuth(Settings.SessionId!, Settings.UserId!)).ConfigureAwait(false);

                    if (!result)
                    {
                        Settings.SessionId = null;
                        Dispatcher.UIThread.Post(() => Login.ShowError(Localized.Login.SidExpired));

                        return;
                    }

                    Dispatcher.UIThread.Post(() => ShowMainWindow(new LoginResult(Settings.Username!, Settings.UserId!, Settings.SessionId!)));
                });
        }
        public LoginWindow(LocalizedString error) : this(false) => Login.ShowError(error);


        async ValueTask<OperationResult<LoginResult>> TryAuthenticateExternal(LoginType type)
        {
            Login.StartLoginAnimation(Localized.Login.Waiting);

            var source = new CancellationTokenSource();
            WaitingExternalAuths.Add(source);
            _ = Task.Delay(10_000).ContinueWith(_ => Dispatcher.UIThread.Post(Login.UnlockButtons), source.Token);


            var auth = await Api.AuthenticateExternalAsync(type, source.Token);
            if (!auth)
            {
                source.Cancel();
                Dispatcher.UIThread.Post(Login.UnlockButtons);
            }

            return auth;
        }
        async ValueTask<OperationResult<LoginResult>> TryAuthenticate(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login)) return OperationResult.Err(Localized.Login.EmptyLogin);
            if (string.IsNullOrEmpty(password)) return OperationResult.Err(Localized.Login.EmptyPassword);

            Login.StartLoginAnimation(Localized.Login.Loading);

            var auth = await Api.AuthenticateAsync(login, password, CancellationToken.None);
            if (!auth) return OperationResult.Err(Localized.Login.WrongLoginPassword);

            return auth.Result;
        }
        async Task<OperationResult> CheckAuth(string sid, string userid)
        {
            try
            {
                Login.StartLoginAnimation(Localized.Login.AuthCheck);
                return (await Api.GetUserInfo(sid, CancellationToken.None).ConfigureAwait(false)).GetResult();
            }
            catch (Exception ex) { return OperationResult.Err(ex); }
            finally { Dispatcher.UIThread.Post(Login.StopLoginAnimation); }
        }

        void ShowMainWindow(in LoginResult info)
        {
            if (Login.IsRememberMeToggled) info.SaveToConfig();
            else default(LoginResult).SaveToConfig();

            new MainWindow().Show();
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
            public event Action<string, string> OnPressLogin = delegate { };
            public event Action OnPressForgotPassword = delegate { };
            public event Action<LoginType> OnPressLoginWith = delegate { };

            public TextBox LoginInput => LoginPasswordInput.LoginInput;
            public TextBox PasswordInput => LoginPasswordInput.PasswordInput;
            public bool IsRememberMeToggled
            {
                get => RememberMeSwitch.IsToggled;
                set => RememberMeSwitch.IsToggled = value;
            }

            readonly LoginPasswordInputUI LoginPasswordInput;
            readonly TextBlock ErrorText;
            readonly LoginStatusUI LoginStatus;
            readonly ExternalLoginButtonsUI ExternalLoginButtons;
            readonly RememberMeSwitchUI RememberMeSwitch;
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

                var buttonsAndRemember = new Panel { Margin = new Thickness(30, 0) };
                buttonsAndRemember.Children.Add(ExternalLoginButtons = new ExternalLoginButtonsUI());
                buttonsAndRemember.Children.Add(RememberMeSwitch = new RememberMeSwitchUI());


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

                ExternalLoginButtons.HorizontalAlignment = HorizontalAlignment.Left;
                ExternalLoginButtons.Width = 160;
                ExternalLoginButtons.OnPressLogin += t => OnPressLoginWith(t);

                RememberMeSwitch.HorizontalAlignment = HorizontalAlignment.Right;

                LoginPasswordInput.Margin = new Thickness(30, 0);
                forgotPasswordButton.Margin = new Thickness(0, 0, 0, 50);

                LoginButton.Width = 157;
                LoginButton.Height = 38;
                LoginButton.Text = Localized.Login.Button;
                LoginButton.FontWeight = (FontWeight) 700;
                LoginButton.MaxWidth = forgotPasswordButton.MaxWidth = 157;
                LoginButton.Background = Colors.Accent;
                LoginButton.HoverBackground = Colors.DarkDarkGray;

                LoginButton.OnClick += () => OnPressLogin(LoginInput.Text, PasswordInput.Text);
                forgotPasswordButton.OnClick += () => OnPressForgotPassword();

                Content = grid;

                KeyDown += (_, e) =>
                {
                    if (e.Key != Key.Enter) return;
                    if (!LoginInput.IsFocused && !PasswordInput.IsFocused) return;

                    OnPressLogin(LoginInput.Text, PasswordInput.Text);
                };
            }

            public void LockButtons() => LoginButton.IsEnabled = ExternalLoginButtons.IsEnabled = false;
            public void UnlockButtons() => LoginButton.IsEnabled = ExternalLoginButtons.IsEnabled = true;
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

            class ExternalLoginButtonsUI : UserControl
            {
                public event Action<LoginType> OnPressLogin = delegate { };

                public ExternalLoginButtonsUI()
                {
                    #region icons

                    const string yapath = @"M 5.703125 0.38867188 C 1.081962 0.80453588 -0.080191906 7.6430096 3.5136719 10.173828
                        L 0.50976562 16.796875 L 3.4882812 16.796875 L 6.1386719 10.888672 L 6.6972656 10.888672 L 6.6972656 16.796875
                        L 9.3730469 16.796875 L 9.3984375 0.39453125 L 6.671875 0.39453125 C 6.3334215 0.36198505 6.0112025 0.36094761
                        5.703125 0.38867188 z M 6.1386719 2.6875 C 6.3139771 2.6893829 6.5003943 2.7225039 6.6972656 2.7890625 L 6.6972656
                        8.5195312 C 3.341276 9.4260765 3.5090929 2.6592561 6.1386719 2.6875 z ";

                    const string vkpath = @"M18.0191 0.995034C18.1402 0.555581 18.0191 0.232143 17.4364 0.232143H15.5081C15.017 0.232143
                        14.7911 0.509878 14.67 0.819253C14.67 0.819253 13.6878 3.38917 12.2998 5.05558C11.8512 5.53722 11.645 5.69191 11.3995
                        5.69191C11.2783 5.69191 11.0917 5.53722 11.0917 5.09777V0.995034C11.0917 0.46769 10.9542 0.232143 10.5483 0.232143H7.51672C7.20898
                        0.232143 7.02565 0.478237 7.02565 0.706753C7.02565 1.20597 7.7197 1.32199 7.79172 2.72824V5.7798C7.79172 6.44777 7.68041 6.57082
                        7.43487 6.57082C6.78011 6.57082 5.18904 3.99035 4.24618 1.03722C4.0563 0.464175 3.8697 0.232143 3.37535 0.232143H1.44708C0.897076
                        0.232143 0.785767 0.509878 0.785767 0.819253C0.785767 1.36769 1.44053 4.0923 3.83368 7.6923C5.42803 10.1497 7.67386 11.4821 9.71672
                        11.4821C10.9444 11.4821 11.095 11.1868 11.095 10.6771C11.095 8.32863 10.9837 8.10714 11.5992 8.10714C11.884 8.10714 12.3751 8.26183
                        13.5209 9.4466C14.8304 10.8528 15.0465 11.4821 15.7798 11.4821H17.7081C18.2581 11.4821 18.5364 11.1868 18.3759 10.6032C18.0093 9.37628
                        15.531 6.85207 15.4197 6.68332C15.1349 6.28957 15.2167 6.11378 15.4197 5.76222C15.423 5.75871 17.7768 2.20089 18.0191 0.995034Z";

                    const string fpath = @"M8.77487 10.5L9.25098 7.39754H6.27409V5.38426C6.27409 4.53549 6.68994 3.70815 8.02319
                        3.70815H9.37654V1.06674C9.37654 1.06674 8.14842 0.857143 6.9742 0.857143C4.52264 0.857143 2.92018 2.34308 2.92018
                        5.03304V7.39754H0.195068V10.5H2.92018V18H6.27409V10.5H8.77487Z";

                    const string gpath = @"M15.042 8.60335C15.042 12.8673 12.122 15.9018 7.80988 15.9018C3.67551 15.9018 0.33667 12.5629 0.33667
                        8.42857C0.33667 4.2942 3.67551 0.955357 7.80988 0.955357C9.82283 0.955357 11.5164 1.69364 12.8212 2.91105L10.7871 4.86674C8.12629
                        2.29933 3.1783 4.2279 3.1783 8.42857C3.1783 11.0352 5.26055 13.1475 7.80988 13.1475C10.769 13.1475 11.878 11.0261 12.0527
                        9.92623H7.80988V7.3558H14.9245C14.9938 7.7385 15.042 8.10614 15.042 8.60335Z";

                    #endregion

                    var yaButton = new ExternalLoginButtonUI(yapath) { BorderPadding = new Thickness(0, 0, 3, 0) };
                    var vkButton = new ExternalLoginButtonUI(vkpath) { BorderPadding = new Thickness(0, 0, 2, 0) };
                    var fButton = new ExternalLoginButtonUI(fpath);
                    var gButton = new ExternalLoginButtonUI(gpath) { BorderPadding = new Thickness(0, 0, 1, 0) };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition());

                    grid.Children.Add(yaButton);
                    grid.Children.Add(vkButton);
                    grid.Children.Add(fButton);
                    grid.Children.Add(gButton);

                    Grid.SetColumn(vkButton, 1);
                    Grid.SetColumn(fButton, 2);
                    Grid.SetColumn(gButton, 3);

                    yaButton.OnClick += () => OnPressLogin(LoginType.Yandex);
                    vkButton.OnClick += () => OnPressLogin(LoginType.VKontakte);
                    fButton.OnClick += () => OnPressLogin(LoginType.Facebook);
                    gButton.OnClick += () => OnPressLogin(LoginType.Google);

                    Content = grid;
                }


                class ExternalLoginButtonUI : ClickableControl
                {
                    public Thickness BorderPadding { set => Border.Padding = value; }

                    readonly Border Border;

                    public ExternalLoginButtonUI(string pathstr)
                    {
                        Width = Height = 30;

                        var path = new Avalonia.Controls.Shapes.Path
                        {
                            Data = Geometry.Parse(pathstr)
                        };
                        path.Stroke = path.Fill = Colors.GrayButton;
                        path.HorizontalAlignment = HorizontalAlignment.Center;
                        path.VerticalAlignment = VerticalAlignment.Center;

                        Border = new Border
                        {
                            BorderThickness = new Thickness(2),
                            CornerRadius = new CornerRadius(Width),
                            BorderBrush = Colors.GrayButton,
                            Child = path
                        };

                        Content = Border;


                        PointerEnter += (_, _) =>
                        {
                            if (IsEnabled) path.Stroke = path.Fill = Border.BorderBrush = Colors.Accent;
                        };
                        PointerLeave += (_, _) =>
                        {
                            if (IsEnabled) path.Stroke = path.Fill = Border.BorderBrush = Colors.GrayButton;
                        };
                        this.GetObservable(IsEnabledProperty).Subscribe(v =>
                            Border.BorderBrush = path.Stroke = path.Fill = v ? Colors.GrayButton : Colors.BorderColor);
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