using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace NodeUI
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
                UICache.StartUpdatingStats();
                UICache.StartUpdatingState().Consume();

                MainTheme.Apply(Resources, Styles);

                if (!Environment.GetCommandLineArgs().Contains("hidden"))
                    SetMainWindow(desktop);
            }
        }

        public static Window SetMainWindow(IClassicDesktopStyleApplicationLifetime lifetime) =>
            lifetime.MainWindow =
                NodeGlobalState.Instance.AuthInfo?.SessionId is null
                ? new LoginWindow()
                : new MainWindow();
    }
}