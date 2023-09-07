using System.Reflection;
using Autofac.Builder;
using Autofac.Core;

namespace Node.Common;

public class AutoServiceRegistrator : IRegistrationSource
{
    public bool IsAdapterForIndividualComponents => false;

    public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
    {
        if (service is not IServiceWithType typed || typed.ServiceType.GetCustomAttribute<AutoRegisteredServiceAttribute>() is not { } attribute)
            return Enumerable.Empty<IComponentRegistration>();

        var registration = RegistrationBuilder.ForType(typed.ServiceType);

        if (attribute.SingleInstance)
            registration = registration.SingleInstance();
        else registration = registration.InstancePerDependency();

        return new[] { registration.CreateRegistration() };
    }
}
