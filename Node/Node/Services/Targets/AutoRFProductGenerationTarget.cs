namespace Node.Services.Targets;

public class AutoRFProductGenerationTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<AutoRFProductGenerator>()
            .SingleInstance();
    }

    public required AutoRFProductGenerator Generator { get; init; }
}
