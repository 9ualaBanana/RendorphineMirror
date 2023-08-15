using Microsoft.Extensions.Localization;
using System.Text;
using Telegram.MPlus.Clients;
using Telegram.MPlus.Security;
using Telegram.TrialUsers;

namespace Telegram.Localization.Resources;

public abstract partial class LocalizedText
{
    public class Authentication : LocalizedText
    {
        const string Guest = nameof(Guest);

        readonly TrialUsersMediatorClient _trialUsersMediatorClient;
        readonly MPlusClient _mPlusClient;

        public Authentication(
            TrialUsersMediatorClient trialUsersMediatorClient,
            MPlusClient mPlusClient,
            IStringLocalizer<Authentication> localizer)
            : base(localizer)
        {
            _trialUsersMediatorClient = trialUsersMediatorClient;
            _mPlusClient = mPlusClient;
        }

        internal string Start(string loginCommand) => Localizer[nameof(Start), loginCommand];

        internal string Usage => Localizer[nameof(Usage)];

        internal string BrowserAuthenticationButton => Localizer[nameof(BrowserAuthenticationButton)];

        internal string LogInAsGuestButton => Localizer[nameof(LogInAsGuestButton)];

        string Balance(int balance) => Localizer[nameof(Balance), balance];

        internal string Failure => Localizer[nameof(Failure)];

        string LoggedInAs(string user) => Localizer[nameof(LoggedInAs), user];

        string AlreadyLoggedInAs(string user) => Localizer[nameof(AlreadyLoggedInAs), user];

        internal string LoggedOut => Localizer[nameof(LoggedOut)];

        internal string WrongSyntax(string loginCommand, string correctSyntax)
            => Localizer[nameof(WrongSyntax), loginCommand, correctSyntax];


        internal async Task<string> SuccessfulLogInAsync(ChatId chatId, MPlusIdentity identity, CancellationToken cancellationToken)
        {
            var balance = await _mPlusClient.TaskLauncher.RequestBalanceAsync(identity.SessionId, cancellationToken);
            bool isTrialUser = await _trialUsersMediatorClient.IsAuthenticatedAsync(chatId, identity.UserId, cancellationToken);

            var message = new StringBuilder();
            message.AppendLine($"{LoggedInAs(isTrialUser ? Guest : identity.Email)}")
                .AppendLine();
            if (!isTrialUser)
                message.AppendLine($"{Balance(balance.RealBalance)}")
                    .AppendLine();
            message.AppendLine($"{Usage}");

            return message.ToString();
        }

        internal async Task<string> AlreadyLoggedInAsync(ChatId chatId, MPlusIdentity identity, CancellationToken cancellationToken)
        {
            bool isTrialUser = await _trialUsersMediatorClient.IsAuthenticatedAsync(chatId, identity.UserId, cancellationToken);
            return AlreadyLoggedInAs(isTrialUser ? Guest : identity.Email);
        }

        internal string FailedLogIn(Exception exception)
            => $"""
            {Failure}:
            {exception.Message}
            """;
    }
}
