using Telegram.Infrastructure.Bot;

namespace TrialUsersMediator;

public partial record TrialUser
{
    public partial record Info
    {
        /// <summary>
        /// Represents a link table that contains references to one or more concrete <see cref="Info"/> entities.
        /// </summary>
        public record Entity
        {
            public TrialUser.Entity TrialUser { get; private set; } = default!;

            public Info.Telegram.Entity? Telegram { get; private set; } = default!;

            internal static Entity From(TelegramBot.User.LoginWidgetData telegramInfo)
                => new() { Telegram = new Info.Telegram.Entity(telegramInfo) };
        }
    }
}
