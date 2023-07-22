using Microsoft.Extensions.Options;
using Telegram.MPlus.Clients;
using Telegram.MPlus.Security;

namespace TrialUsersMediator;

public partial record TrialUser
{

    public partial record Identity
    {
        internal MPlusIdentity _ { get; private set; } = default!;

        readonly MPlusTaskManagerClient _client;
        readonly Credentials _options;

        public Identity(MPlusTaskManagerClient client, IOptions<Credentials> credentials)
        {
            _client = client;
            _options = credentials.Value;
        }

        internal async Task ObtainAsync()
            => _ = await _client.AuthenticateAsyncUsing(_options.Email, _options.Password);
    }
}
