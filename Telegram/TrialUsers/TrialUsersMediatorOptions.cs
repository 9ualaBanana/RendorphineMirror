namespace Telegram.TrialUsers;

public record TrialUsersMediatorOptions
{
    internal const string Configuration = "TrialUsersMediator";

    public Uri Host { get; init; } = default!;
}

public static class TrialUsersMediatorOptionsExtensions
{
    public static IServiceCollection ConfigureTrialUsersMediatorOptions(this IServiceCollection services)
    {
        services.AddOptions<TrialUsersMediatorOptions>()
            .BindConfiguration(TrialUsersMediatorOptions.Configuration);
        return services;
    }
}
