namespace Telegram.TrialUsers;

static class TrialUsersExtensions
{
    internal static IServiceCollection AddTrialUsers(this IServiceCollection services)
    {
        services.AddHttpClient<TrialUsersMediatorClient>();
        services.ConfigureTrialUsersMediatorOptions();

        return services;
    }
}
