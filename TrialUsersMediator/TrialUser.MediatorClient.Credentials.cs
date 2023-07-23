using System.ComponentModel.DataAnnotations;

namespace TrialUsersMediator
{
    public partial record TrialUser
    {
        public partial class MediatorClient
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
}
