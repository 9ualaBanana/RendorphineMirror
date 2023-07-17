using Microsoft.Extensions.Localization;

namespace Telegram.Localization.Resources;

public abstract partial class LocalizedText
{
    public class Authentication : LocalizedText
    {
        public Authentication(IStringLocalizer<Authentication> localizer)
            : base(localizer)
        {
        }

        internal string Start(string loginCommand) => Localizer[nameof(Start), loginCommand];

        internal string HowToUse => Localizer[nameof(HowToUse)];

        internal string BrowserAuthenticationButton => Localizer[nameof(BrowserAuthenticationButton)];

        internal string LoginAsGuestButton => Localizer[nameof(LoginAsGuestButton)];

        internal string Balance(int balance) => Localizer[nameof(Balance), balance];

        internal string Failure => Localizer[nameof(Failure)];

        internal string AlreadyLoggedIn => Localizer[nameof(AlreadyLoggedIn)];

        internal string LoggedIn => Localizer[nameof(LoggedIn)];

        internal string LoggedOut => Localizer[nameof(LoggedOut)];

        internal string WrongSyntax(string loginCommand, string correctSyntax)
            => Localizer[nameof(WrongSyntax), loginCommand, correctSyntax];
    }
}
