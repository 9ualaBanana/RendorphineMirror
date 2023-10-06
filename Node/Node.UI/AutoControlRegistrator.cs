using Autofac.Builder;
using Autofac.Core;

namespace Node.UI;

public class AutoControlRegistrator : IRegistrationSource
{
    public bool IsAdapterForIndividualComponents => false;

    public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
    {
        if (service is not IServiceWithType typed || !typed.ServiceType.IsAssignableTo(typeof(Control)))
            return Enumerable.Empty<IComponentRegistration>();

        return new[]
        {
            RegistrationBuilder.ForType(typed.ServiceType)
                .InstancePerDependency()
                .CreateRegistration(),
        };
    }
}
