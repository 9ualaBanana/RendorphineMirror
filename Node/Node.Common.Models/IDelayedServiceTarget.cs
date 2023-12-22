using Autofac.Core;

namespace Node.Common.Models;

public interface IDelayedServiceTarget
{
    static abstract IEnumerable<IComponentRegistration> CreateRegistrations();

    async Task ExecuteAsync() { }
    void Activated() { }
}
