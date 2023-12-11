namespace Node.Services.Targets;

public class PublishMainTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required ReleaseMainTarget Release { get; init; }
    public required SystemTimerStartedTarget SystemTimerStartedTarget { get; init; }
}
