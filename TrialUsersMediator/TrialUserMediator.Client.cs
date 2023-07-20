using Telegram.MPlus.Clients;

namespace TrialUsersMediator;

public partial class TrialUserMediator
{
    public class Client
    {
        internal readonly MPlusClient _;
        readonly TrialUser.Identity _identity;

        // TODO: Move to IOptions.
        const string Email = "netherspite123@gmail.com";
        const string Password = "nhbfkmysqgfhjkm";

        public Client(MPlusClient mPlusClient, TrialUser.Identity identity)
        {
            _ = mPlusClient;
            _identity = identity;
        }

        internal async Task InitializeAsync()
        {
            _identity._ = await _.TaskManager.AuthenticateAsyncUsing(Email, Password);
        }
    }
}
