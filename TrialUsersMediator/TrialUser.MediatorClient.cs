using Telegram.MPlus.Clients;
using Telegram.MPlus.Security;

namespace TrialUsersMediator;

public partial record TrialUser
{
    public class MediatorClient : MPlusClient
    {
        readonly internal MPlusIdentity Identity;

        public MediatorClient(
            MPlusTaskManagerClient taskManager,
            MPlusTaskLauncherClient taskLauncher,
            Identity identity)
            : base(taskManager, taskLauncher)
        {
            Identity = identity._;
        }
    }
}
