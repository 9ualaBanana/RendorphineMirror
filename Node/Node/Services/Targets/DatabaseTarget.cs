namespace Node.Services.Targets;

public class DatabaseTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterInstance(Settings.Instance)
            .SingleInstance();

        builder.RegisterType<NodeSettingsInstance>()
            .AsImplementedInterfaces()
            .SingleInstance();
    }
}
