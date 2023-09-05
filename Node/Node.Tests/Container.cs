using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace Node.Tests;

public static class Container
{
    public static readonly IContainer Instance = CreateBuilder().Build();

    public static ContainerBuilder CreateBuilder()
    {
        var builder = new ContainerBuilder();

        // logging
        builder.Populate(new ServiceCollection().With(services => services.AddLogging(l => l.AddNLog())));

        return builder;
    }
}
