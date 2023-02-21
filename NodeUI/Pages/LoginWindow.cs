using Avalonia.Controls.ApplicationLifetimes;

namespace NodeUI.Pages
{
    public class LoginWindow : LoginWindowUI
    {
        CancellationTokenSource? WebAuthToken;

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


            async Task<OperationResult> authenticate(string? login, string? password, LoginType loginType)
            {
                var authres = await auth(login, password, loginType);
                if (!authres) Dispatcher.UIThread.Post(() => Login.ShowError(authres.AsString()));

                return authres;
            }
            async Task<OperationResult> auth(string? login, string? password, LoginType loginType)
            {
                Login.StartLoginAnimation("login.loading");
                using var _ = new FuncDispose(() => Dispatcher.UIThread.Post(Login.StopLoginAnimation));

                OperationResult auth;
                if (loginType == LoginType.Normal)
                {
                    if (string.IsNullOrWhiteSpace(login))
                        return OperationResult.Err("login.empty_login");
                    if (string.IsNullOrEmpty(password))
                        return OperationResult.Err("login.empty_password");

                    auth = await LocalApi.Default.Post("login", "Logging in", (nameof(login), login), (nameof(password), password));
                }
                else if (loginType == LoginType.Slave)
                {
                    if (string.IsNullOrWhiteSpace(login))
                        return OperationResult.Err("login.empty_login");

                    auth = await LocalApi.Default.Post("autologin", "Autologging in", (nameof(login), login));
                }
                else if (loginType == LoginType.Web)
                {
                    if (WebAuthToken is null || WebAuthToken.IsCancellationRequested)
                    {
                        Task.Delay(5000).ContinueWith(_ => Dispatcher.UIThread.Post(() =>
                        {
                            Login.UnlockButtons();
                            Login.SetMPlusLoginButtonText("Cancel web login");
                        })).Consume();

                        WebAuthToken = new();
                        auth = await LocalApi.Default.WithCancellationToken(WebAuthToken.Token).Post("weblogin", "Weblogging in");
                        WebAuthToken = null;
                        Login.SetMPlusLoginButtonText("Login via M+");
                    }
                    else
                    {
                        WebAuthToken.Cancel();
                        WebAuthToken = null;
                        return false;
                    }
                }
                else throw new InvalidOperationException("Unknown value of LoginType: " + loginType);

                // https://microstock.plus/oauth2/authorize?clientid=001&redirecturl=http://127.0.0.1:9999/
                return auth;
            }


            Login.OnPressLogin += (login, password, slave) => authenticate(login, password, slave ? LoginType.Slave : LoginType.Normal).Consume();
            Login.OnPressWebLogin += () => authenticate(null, null, LoginType.Web).Consume();
            Login.OnPressForgotPassword += () => Process.Start(new ProcessStartInfo("https://accounts.stocksubmitter.com/resetpasswordrequest") { UseShellExecute = true });
        }


        enum LoginType { Normal, Slave, Web }
    }
    public class LoginWindowUI : Window
    {
        protected readonly LoginControl Login;

        public LoginWindowUI()
        {
            Login = new LoginControl();

            Content = new Grid()
            {
                ColumnDefinitions = ColumnDefinitions.Parse("45* 55*"),
                Children =
                {
                    new Border()
                    {
                        Child = new HelloImage(),
                        BoxShadow = new BoxShadows(new BoxShadow { Blur = 14, Color = new Color(64, 0, 0, 0) })
                    }.WithColumn(0),
                    Login.WithColumn(1),
                },
            };
        }


        class HelloImage : UserControl
        {
            public HelloImage()
            {
                Content = new Panel()
                {
                    Children =
                    {
                        new Image()
                        {
                            Stretch = Stretch.Fill,
                            Source = new Bitmap(Resource.LoadStream(this, "img.login_image.jpg")),
                        },
                        new TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 20,
                            Foreground = Colors.From(38, 59, 71),
                            MaxWidth = 200,
                            Margin = new Thickness(111, 111, 0, 206),
                        }.Bind("login.welcome"),
                    }
                };
            }
        }
        protected class LoginControl : UserControl
        {
            public event Action OnPressWebLogin = delegate { };
            public event Action<string, string, bool> OnPressLogin = delegate { };
            public event Action OnPressForgotPassword = delegate { };

            public TextBox LoginInput => LoginPasswordInput.LoginInput;
            public TextBox PasswordInput => LoginPasswordInput.PasswordInput;

            readonly LoginPasswordInputUI LoginPasswordInput;
            readonly TextBlock ErrorText;
            readonly LoginStatusUI LoginStatus;
            readonly MPButton LoginButton, MPlusLoginButton;

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



                LoginPasswordInput = new LoginPasswordInputUI()
                {
                    Margin = new Thickness(30, 0),
                };

                LoginStatus = new LoginStatusUI()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                LoginButton = new MPButton()
                {
                    Width = 157,
                    Height = 38,
                    Text = "login.button",
                    FontWeight = (FontWeight) 700,
                    MaxWidth = 157,
                    Background = Colors.Accent,
                    HoverBackground = Colors.DarkDarkGray,
                    OnClick = () => OnPressLogin(LoginInput.Text, PasswordInput.Text, slavecheckbox.IsChecked == true),
                };

                MPlusLoginButton = new MPButton()
                {
                    Text = "Login via M+",
                    OnClick = () => OnPressWebLogin(),
                };


                Content = new Grid()
                {
                    RowDefinitions = RowDefinitions.Parse("40* 60* 110* 20* 30* 30* 40* 20*"),
                    Children =
                    {
                        new LoginTopBarUI().WithRow(0),
                        ErrorText.WithRow(1),
                        LoginStatus.WithRow(1),
                        LoginPasswordInput.WithRow(2),
                        buttonsAndRemember.WithRow(4),
                        MPlusLoginButton.WithRow(5),
                        LoginButton.WithRow(6),
                        new ForgotPasswordButtonUI()
                        {
                            Margin = new Thickness(0, 0, 0, 50),
                            OnClick = () => OnPressForgotPassword(),
                            MaxWidth = LoginButton.MaxWidth,
                        }.WithRow(7),
                    }
                };


                HideError();

                {
                    PasswordInput.Transitions ??= new();
                    PasswordInput.Transitions.Add(new ThicknessTransition() { Property = Control.MarginProperty, Duration = TimeSpan.FromSeconds(1) });
                    slavecheckbox.Subscribe(CheckBox.IsCheckedProperty, c => PasswordInput.Margin = c != true ? new Thickness(0, 0, 0, 0) : new Thickness(0, -100, 0, 0));
                }

                KeyDown += (_, e) =>
                {
                    if (e.Key != Key.Enter) return;
                    if (!LoginInput.IsFocused && !PasswordInput.IsFocused) return;

                    OnPressLogin(LoginInput.Text, PasswordInput.Text, slavecheckbox.IsChecked == true);
                };
            }

            public void SetMPlusLoginButtonText(LocalizedString text) => MPlusLoginButton.Text = text;
            public void LockButtons() => LoginButton.IsEnabled = MPlusLoginButton.IsEnabled = false;
            public void UnlockButtons() => LoginButton.IsEnabled = MPlusLoginButton.IsEnabled = true;
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
                    }.Bind("login.title");

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
                    IsVisible = false;

                    TextBlock = new TextBlock()
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10, 0),
                        FontSize = 16,
                        Foreground = Colors.DarkText,
                    }.Bind("login.loading");

                    Content = new Grid()
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("25 *"),
                        Children =
                        {
                            new LoadCircle().WithColumn(0),
                            TextBlock.WithColumn(1),
                        },
                    };
                }
            }
            class LoginPasswordInputUI : UserControl
            {
                public readonly TextBox LoginInput;

                public TextBox PasswordInput => EyeTextBox.TextBox;
                readonly EyeTextBoxUI EyeTextBox;

                public LoginPasswordInputUI()
                {
                    EyeTextBox = new EyeTextBoxUI();

                    LoginInput = new TextBox()
                    {
                        FontSize = PasswordInput.FontSize = 16,
                        VerticalContentAlignment = PasswordInput.VerticalContentAlignment = VerticalAlignment.Center,
                        Foreground = PasswordInput.Foreground = Colors.DarkText,
                        BorderThickness = PasswordInput.BorderThickness = new Thickness(0),
                        Background = PasswordInput.Background = Colors.Transparent,
                        Padding = PasswordInput.Padding = new Thickness(20, 0, 0, 0),
                        Cursor = PasswordInput.Cursor = new Cursor(StandardCursorType.Ibeam),
                    }.Bind(TextBox.WatermarkProperty, "login.email");

                    Content = new Border
                    {
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(4),
                        BorderBrush = Colors.BorderColor,
                        Child = new Grid()
                        {
                            RowDefinitions = RowDefinitions.Parse("* 2 *"),
                            Children =
                            {
                                LoginInput.WithRow(0),
                                new Panel { Background = Colors.BorderColor }.WithRow(1),
                                EyeTextBox.WithRow(2),
                            },
                        },
                    };
                }


                class EyeTextBoxUI : UserControl
                {
                    public readonly TextBox TextBox;

                    public EyeTextBoxUI()
                    {
                        TextBox = new TextBox() { PasswordChar = '*' }.Bind(TextBox.WatermarkProperty, "login.password");

                        var eye = new EyeUI();
                        eye.OnToggle += t => TextBox.PasswordChar = t ? default : '*';

                        Content = new Grid()
                        {
                            ColumnDefinitions = ColumnDefinitions.Parse("* 56"),
                            Children =
                            {
                                TextBox.WithColumn(0).WithColumnSpan(2),
                                eye.WithColumn(1),
                            },
                        };
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
                    CheckBox = new SwitchUI()
                    {
                        Width = 13,
                        Height = 13,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    Content = new Grid()
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("* *"),
                        Children =
                        {
                            CheckBox.WithColumn(0),
                            new TextBlock()
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(10, 0, 0, 0),
                            }.Bind("login.remember_me").WithColumn(1),
                        },
                    };
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
                        Content = BackgroundBorder = new Border()
                        {
                            Background = Colors.GrayButton,
                            BorderBrush = Colors.GrayButton,
                            CornerRadius = new CornerRadius(3),
                        };

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
                    }.Bind("login.forgot_password");

                    Content = text;

                    PointerEnter += (_, _) => text.Foreground = Colors.Accent;
                    PointerLeave += (_, _) => text.Foreground = Colors.DarkText;
                }
            }
        }
    }
}