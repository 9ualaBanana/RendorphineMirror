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
        builder.ConfigureTrialUserMediatorClientCredentials();

        return builder;
    }

    static IWebHostBuilder ConfigureTrialUserMediatorClientCredentials(this IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(_ => _.AddJsonFile("credentials.json", optional: false, reloadOnChange: false));

        builder.ConfigureServices(_ => _
            .AddOptions<TrialUser.MediatorClient.Credentials>()
                .BindConfiguration(TrialUser.MediatorClient.Credentials.Configuration)
                // Exception upon validation failure will be thrown only when IOptions.Value property is accessed.
                .ValidateDataAnnotations());

        return builder;
    }
}
