using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.MPlus.Clients;

namespace Telegram.MPlus;

public static class MPlusExtensions
{
    public static IServiceCollection AddMPlusClient(this IServiceCollection services)
    {
        services.AddHttpClient<MPlusTaskManagerClient>();
        services.AddHttpClient<MPlusTaskLauncherClient>();
        services.AddHttpClient<StockSubmitterClient>();
        services.TryAddScoped<MPlusClient>();

        return services;
    }
}
