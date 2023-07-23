using Telegram.Infrastructure.Bot;

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
            .BindConfiguration(TrialUsersMediatorOptions.Configuration)
            .Validate(_ => _.Host.OriginalString.EndsWith('/'),
                $"{nameof(TelegramBot.Options.Host)} must end with a path separator.")
            .ValidateOnStart();
        return services;
    }
}
