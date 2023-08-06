namespace Node.Common.Models;

public static class ContainerExtensions
{
    public static ILogger ResolveLogger<T>(this ILifetimeScope container, T obj) where T : notnull =>
        (ILogger) container.Resolve(typeof(ILogger<>).MakeGenericType(obj.GetType()));
    public static ILogger ResolveLogger(this ILifetimeScope container, Type type) =>
        (ILogger) container.Resolve(typeof(ILogger<>).MakeGenericType(type));

    public static ILifetimeScope ResolveForeign<T>(this ILifetimeScope container, Type type, out T obj) where T : notnull
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
}
