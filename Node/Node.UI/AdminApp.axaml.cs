using Autofac;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Node.UI;

public class AdminApp : AppBase
{
    public static IContainer Initialize(ContainerBuilder builder)
    {
        builder.RegisterType<AdminApp>()
            .As<Application>()
            .SingleInstance();

        builder.RegisterType<AdminApis>()
            .As<Apis>()
            .SingleInstance();

        builder.RegisterType<UISettings>()
            .SingleInstance();

        builder.RegisterSource<AutoControlRegistrator>();
        return builder.Build();
    }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("Non-desktop platforms are not supported");

        // MainTheme.Apply(Resources, Styles);
        desktop.MainWindow = Container.Resolve<AdminWindow>();
        base.OnFrameworkInitializationCompleted();
    }


    record AdminApis : Apis
    {
        public override string SessionId { get; init; }

        public AdminApis(Api api, DataDirs dirs) : base(api, true)
        {
            var db = new Database(Path.Combine(dirs.Data, "config.db"));
            var sesionid = (new DatabaseValue<AuthInfo?>(db, nameof(AuthInfo), default).Value?.SessionId) ?? throw new Exception("No sessionid stored");
            SessionId = sesionid;
        }
    }
}
