using Telegram.Infrastructure.Bot;

namespace TrialUsersMediator;

public partial record TrialUser
{
    public partial record Info
    {
        /// <summary>
        /// <see cref="Info"/> wrapper for <see cref="TelegramBot.User.LoginWidgetData"/>.
        /// </summary>
        public partial record Telegram : TelegramBot.User.LoginWidgetData
        {
            Telegram() { }
            protected Telegram(TelegramBot.User.LoginWidgetData telgramInfo)
                : base(telgramInfo)
            {
            }
        }
    }
}
