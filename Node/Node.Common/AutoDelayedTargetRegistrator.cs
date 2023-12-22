using System.Reflection;
using Autofac.Builder;
using Autofac.Core;

namespace Node.Common;

/// <summary>
/// <see cref="IRegistrationSource"/> that automatically registers all <see cref="IDelayedServiceTarget"/> that has the requested property.
/// </summary>
public class AutoDelayedTargetRegistrator : IRegistrationSource
{
    public bool IsAdapterForIndividualComponents => false;

    readonly Dictionary<Assembly, IReadOnlyCollection<Type>> CachedTargets = [];
    readonly List<Type> LoadedTargets = [];

    public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
    {
        if (service is not IServiceWithType typed)
            return [];

        var assembly = typed.ServiceType.Assembly;
        if (!CachedTargets.TryGetValue(assembly, out var targets))
        {
            targets = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.IsAssignableTo(typeof(IDelayedServiceTarget)))
                .ToArray();

            CachedTargets[assembly] = targets;
        }

        targets = targets
            .Where(t =>
                !LoadedTargets.Contains(t)
                && (
                    t.IsAssignableTo(typeof(IDelayedServiceTarget))
                    || t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.PropertyType == typed.ServiceType)
                )
            )
            .ToArray();

        if (targets.Count == 0)
            return [];

        LoadedTargets.AddRange(targets);
        return registerTargets(targets);


        static IEnumerable<IComponentRegistration> registerTargets(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                var reg = RegistrationBuilder.ForType(type)
                    .SingleInstance()
                    .OnActivating(async l =>
                    {
                        var logger = l.Context.ResolveLogger(l.Instance.GetType());

                        logger.LogInformation($"Resolved delayed target {l.Instance}");
                        await ((IDelayedServiceTarget) l.Instance).ExecuteAsync().ConfigureAwait(false);
                        logger.LogInformation($"Reached delayed target {l.Instance}");
                    })
                    .OnActivated(l =>
                    {
                        var logger = l.Context.ResolveLogger(l.Instance.GetType());

                        logger.LogInformation($"Activating delayed target {l.Instance}");
                        ((IDelayedServiceTarget) l.Instance).Activated();
                        logger.LogInformation($"Activated delayed target {l.Instance}");
                    });

                if (type == typeof(Init))
                    reg = reg
                        .AsSelf()
                        .AutoActivate();

                yield return reg.CreateRegistration();
            }

            foreach (var type in types)
            {
                var registrations = (IEnumerable<IComponentRegistration>) (type.GetMethod(nameof(IDelayedServiceTarget.CreateRegistrations))?.Invoke(null, [])).ThrowIfNull();
                foreach (var registration in registrations)
                    yield return registration;
            }
        }
    }
}
