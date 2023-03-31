namespace Telegram.MPlus;

static class MPlusExtensions
{
    internal static IServiceCollection AddMPlusClient(this IServiceCollection services)
    {
        services.AddHttpClient<MPlusTaskManagerClient>();
        services.AddHttpClient<MPlusTaskLauncherClient>();
        services.AddHttpClient<StockSubmitterClient>();
        return services.AddScoped<MPlusClient>();
    }
}
