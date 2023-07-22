using System.ComponentModel.DataAnnotations;
using TrialUsersMediator;

namespace TrialUsersMediator
{
    public partial record TrialUser
    {
        public record Credentials
        {
            internal const string Configuration = "Credentials";

            [EmailAddress]
            public string Email { get; init; } = default!;
            public string Password { get; init; } = default!;
        }
    }
}

static class TrialUserCredentialsExtensions
{
    internal static IWebHostBuilder ConfigureTrialUserCredentials(this IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(_ => _.AddJsonFile("credentials.json", optional: false, reloadOnChange: false));

        builder.ConfigureServices(_ => _
            .AddOptions<TrialUser.Credentials>()
                .BindConfiguration(TrialUser.Credentials.Configuration)
                // Exception upon validation failure will be thrown only when IOptions.Value property is accessed.
                .ValidateDataAnnotations());

        return builder;
    } 
}
