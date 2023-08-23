using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
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

            var builder = new ContainerBuilder();

            // logging
            builder.Populate(new ServiceCollection().With(services => services.AddLogging(l => l.AddNLog())));

            builder.RegisterSource<AutoControlInstantiator>();

            builder.RegisterInstance(this)
                .As<App>()
                .SingleInstance();

            builder.RegisterInstance(desktop)
                .SingleInstance();

            builder.RegisterType<MainWindow>();
            builder.RegisterType<LoginWindow>();

            builder.RegisterType<NodeStateUpdater>()
                .SingleInstance();

            builder.RegisterType<MainWindowUpdater>()
                .SingleInstance();

            builder.RegisterType<TrayIndicator>()
                .SingleInstance();

            builder.RegisterInstance(Api.Default)
                .SingleInstance();

            builder.RegisterType<LocalApi>()
                .SingleInstance();

            builder.RegisterType<Startup>()
                .SingleInstance();

            var container = builder.Build();
            container.Resolve<Startup>().Start();

            base.OnFrameworkInitializationCompleted();
        }
    }


    class AutoControlInstantiator : IRegistrationSource
    {
        public bool IsAdapterForIndividualComponents => false;

        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
        {
            if (service is not IServiceWithType typed) yield break;
            if (!typed.ServiceType.IsAssignableTo(typeof(IControl))) yield break;

            yield return RegistrationBuilder.ForType(typed.ServiceType)
                .CreateRegistration();
        }
    }
}