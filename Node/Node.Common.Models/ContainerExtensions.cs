using Autofac.Core;

namespace Node.Common.Models;

public static class ContainerExtensions
{
    public static ILogger ResolveLogger<T>(this IComponentContext container, T obj)
        where T : notnull =>
        container.ResolveLogger(obj.GetType());

    public static ILogger ResolveLogger(this IComponentContext container, Type type) =>
        (ILogger) container.Resolve(typeof(ILogger<>).MakeGenericType(type));

    public static ILifetimeScope ResolveForeign<T>(this ILifetimeScope container, Type type, out T obj)
        where T : notnull
    {
        var ctx = container.BeginLifetimeScope(ctx =>
        {
            ctx.RegisterType(type)
                .As<T>()
                .SingleInstance();
        });

        obj = ctx.Resolve<T>();
        return ctx;
    }


    public static IEnumerable<TKey> GetAllRegisteredKeys<TKey>(this IComponentContext container)
        where TKey : notnull =>
        container.ComponentRegistry.Registrations
            .SelectMany(r => r.Services)
            .OfType<KeyedService>()
            .Where(s => s.ServiceType == typeof(TKey))
            .Select(k => (TKey) k.ServiceKey)
            .Distinct();

    public static IEnumerable<T> ResolveAllKeyed<T, TKey>(this IComponentContext container)
        where T : notnull
        where TKey : notnull =>
        container.GetAllRegisteredKeys<TKey>()
            .Select(key => container.ResolveKeyed<T>(key));
}
