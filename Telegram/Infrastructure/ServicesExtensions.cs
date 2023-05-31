namespace Telegram.Infrastructure;

static class ServicesExtensions
{
    internal static IServiceCollection TryAddTransient_<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(service => service.Is<TService, TImplementation>()))
            services.AddTransient<TService, TImplementation>();

        return services;
    }

    internal static IServiceCollection TryAddTransient_<TService, TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(service => service.Is<TService, TImplementation>()))
            services.AddTransient<TService, TImplementation>(implementationFactory);

        return services;
    }

    internal static IServiceCollection TryAddScoped_<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(service => service.Is<TService, TImplementation>()))
            services.AddScoped<TService, TImplementation>();

        return services;
    }

    internal static IServiceCollection TryAddScoped_<TService, TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(service => service.Is<TService, TImplementation>()))
            services.AddScoped<TService, TImplementation>(implementationFactory);

        return services;
    }

    internal static IServiceCollection TryAddSingleton_<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(service => service.Is<TService, TImplementation>()))
            services.AddSingleton<TService, TImplementation>();

        return services;
    }

    internal static IServiceCollection TryAddSingleton_<TService, TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(service => service.Is<TService, TImplementation>()))
            services.AddSingleton<TService, TImplementation>(implementationFactory);

        return services;
    }

    internal static bool Is<TService, TImplementation>(this ServiceDescriptor serviceDescriptor)
        => serviceDescriptor.ServiceType == typeof(TService)
        && serviceDescriptor.ImplementationType == typeof(TImplementation);
}
