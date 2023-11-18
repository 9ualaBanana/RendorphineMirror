namespace Node.Common;

public interface IServiceTarget
{
    static abstract void CreateRegistrations(ContainerBuilder builder);
    Task ExecuteAsync();
}
