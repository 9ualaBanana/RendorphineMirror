using Autofac.Core;

namespace _3DProductsPublish;

public class _3DProductPublisherTarget : IDelayedServiceTarget
{
    public static IEnumerable<IComponentRegistration> CreateRegistrations() => [];
}
