namespace Telegram.TrialUsers;

public record TrialUsersMediatorOptions
{
    internal const string Configuration = "TrialUsersMediator";

    public Uri Host => _host ??= new(_Host.EndsWith('/') ? _Host : $"{_Host}/");
    Uri? _host;
    string _Host { get; init; } = null!;
}

static class TrialUsersMediatorOptionsExtensions
{
    internal static IServiceCollection ConfigureTrialUsersMediatorOptions(this IServiceCollection services)
    {
        services.AddOptions<TrialUsersMediatorOptions>()
            .BindConfiguration(TrialUsersMediatorOptions.Configuration, _ => _.BindNonPublicProperties = true);
        return services;
    }
}
