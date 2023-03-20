namespace Telegram.MPlus;

static class MPlusExtensions
{
    internal static IServiceCollection AddMPlusClient(this IServiceCollection services)
    {
        services.AddHttpClient<MPlusTaskManagerClient>();
        services.AddHttpClient<MPlusTaskLauncherClient>();
        return services.AddScoped<MPlusClient>();
    }
}
