using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Telegram.Infrastructure.Bot;

namespace TrialUsersMediator;

public partial record TrialUser
{
    public partial record Info
    {
        public partial record Telegram
        {
            public record Entity : TrialUser.Info.Telegram
            {
                public int Id { get; private set; } = default!;

                Entity() { }
                internal Entity(TelegramBot.User.LoginWidgetData telegramInfo)
                    : base(telegramInfo)
                {
                }


                internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
                {
                    public void Configure(EntityTypeBuilder<Entity> telegramInfoEntity)
                    {
                        telegramInfoEntity.ToTable("TrialUsers_Info_Telegram");
                    }
                }
            }
        }
    }
}
