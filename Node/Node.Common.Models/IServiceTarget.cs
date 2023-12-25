namespace Node.Common.Models;

public interface IServiceTarget
{
    static abstract void CreateRegistrations(ContainerBuilder builder);

    async Task ExecuteAsync() { }
    void Activated() { }
}
