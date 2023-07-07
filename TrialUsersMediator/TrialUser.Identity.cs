using Telegram.MPlus.Security;

namespace TrialUsersMediator;

public partial record TrialUser
{
    public record Identity
    {
        internal MPlusIdentity _
        {
            get
            {
                return _identity ?? throw new InvalidOperationException(
                    $"{nameof(TrialUser.Identity)} must be initialized by calling {nameof(TrialUserMediator.Client.InitializeAsync)} before using.");
            }
            set => _identity = value;
        }

        MPlusIdentity? _identity;
    }
}
