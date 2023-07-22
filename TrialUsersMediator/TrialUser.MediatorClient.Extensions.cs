using Telegram.MPlus;

namespace TrialUsersMediator;

static class TrialUserMediatorClientExtensions
{
    internal static IWebHostBuilder ConfigureTrialUserMediator(this IWebHostBuilder builder)
    {
        builder.ConfigureServices(_ => _
            .AddScoped<TrialUser.MediatorClient>()
            .AddSingleton<TrialUser.Identity>()
            .AddMPlusClient());
        builder.ConfigureTrialUserCredentials();

        return builder;
    }
}
