using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace NodeUI
{
    public class App : Application
    {
        public static readonly string AppName, Version;
        public static readonly WindowIcon Icon = new WindowIcon(Resource.LoadStream(typeof(App).Assembly, "img.icon.ico"));

        static App()
        {
            Version = Init.Version;
            AppName = "Renderphine   v" + Version;
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
                this.InitializeTrayIndicator();

                if (UISettings.Language is { } lang) LocalizedString.SetLocale(lang);
                else UISettings.Language = LocalizedString.Locale;

                MainTheme.Apply(Resources, Styles);

                if (!Environment.GetCommandLineArgs().Contains("hidden"))
                    SetMainWindow(desktop);
            }
        }

        public static Window SetMainWindow(IClassicDesktopStyleApplicationLifetime lifetime) =>
            lifetime.MainWindow =
                Settings.SessionId is null
                ? new LoginWindow()
                : new MainWindow();
    }
}