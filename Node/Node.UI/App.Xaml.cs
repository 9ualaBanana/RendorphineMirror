using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace Node.UI
{
    public class App : Application
    {
        public static readonly string AppName, Version;
        public static readonly WindowIcon Icon = new WindowIcon(Resource.LoadStream(typeof(App).Assembly, Environment.OSVersion.Platform == PlatformID.Win32NT ? "img.icon.ico" : "img.tray_icon.png"));

        static App()
        {
            Version = Init.Version;
            AppName = "Renderfin   v" + Version;
        }
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new InvalidOperationException("Non-desktop platforms are not supported");

            Start();
            base.OnFrameworkInitializationCompleted();


            void Start()
            {
                Name = AppName;

                if (UISettings.Language is { } lang) LocalizedString.SetLocale(lang);
                else UISettings.Language = LocalizedString.Locale;

                this.InitializeTrayIndicator();
                MainTheme.Apply(Resources, Styles);

                if (true)
                {
                    desktop.MainWindow = new TurboSquidModelInfoInputWindow(new NodeToUI.Requests.InputTurboSquidModelInfoRequest(
                        ImmutableArray<NodeToUI.Requests.InputTurboSquidModelInfoRequest.ModelInfo>.Empty
                            .Add(new NodeToUI.Requests.InputTurboSquidModelInfoRequest.ModelInfo("Aboba"))
                            .Add(new NodeToUI.Requests.InputTurboSquidModelInfoRequest.ModelInfo("Abeba")),
                        ImmutableDictionary<string, ImmutableArray<string>>.Empty
                            .Add("eps", ImmutableArray.Create("eps1", "eps2"))
                            .Add("blend", ImmutableArray.Create("blend1", "blend2"))
                        ), null!);
                    return;
                }

                if (Environment.GetCommandLineArgs().Contains("registryeditor"))
                {
                    desktop.MainWindow = new Window() { Content = new Pages.MainWindowTabs.JsonRegistryTab(), };
                    return;
                }

                NodeStateUpdater.Start();
                NodeGlobalState.Instance.BAuthInfo.SubscribeChanged(() => Dispatcher.UIThread.Post(() => SetMainWindow(desktop).Show()));

                if (!Environment.GetCommandLineArgs().Contains("hidden"))
                    SetMainWindow(desktop);
            }
        }

        public static Window SetMainWindow(IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow is MainWindow && NodeStateUpdater.IsConnectedToNode.Value && NodeGlobalState.Instance.AuthInfo?.SessionId is not null)
                return lifetime.MainWindow;

            lifetime.MainWindow?.Hide();
            return lifetime.MainWindow =
                (!NodeStateUpdater.IsConnectedToNode.Value)
                ? new InitializingWindow()
                : NodeGlobalState.Instance.AuthInfo?.SessionId is null
                    ? new LoginWindow()
                    : new MainWindow();
        }
    }
}