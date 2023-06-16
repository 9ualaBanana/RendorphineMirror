using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Telegram.Infrastructure.Bot;

namespace TrialUsersMediator.Persistence;

public partial record TrialUser
{
    internal partial record Info
    {
        public record Telegram : TelegramBot.User.LoginWidgetData
        {
            protected Telegram(TelegramBot.User.LoginWidgetData wrappedTelegramInfo)
                : base(wrappedTelegramInfo)
            {
            }
            public Telegram() { }


            public record Entity : Info.Telegram
            {
                internal TrialUser.Info.Entity TrialUserInfo { get; private set; } = default!;

                Entity(TelegramBot.User.LoginWidgetData wrappedTelegramInfo)
                    : base(wrappedTelegramInfo)
                {
                }
                public Entity() { }


                internal class Wrapper
                {
                    internal static Entity For(TelegramBot.User.LoginWidgetData telegramInfo)
                        => new(telegramInfo);
                }


                internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
                {
                    public void Configure(EntityTypeBuilder<Entity> telegramInfoEntity)
                    {
                        telegramInfoEntity.ToTable("TrialUsers_Info_Telegram");

                        const string Id = nameof(Id);
                        telegramInfoEntity.Property<int>(Id).ValueGeneratedOnAdd();
                        telegramInfoEntity.HasKey(Id);

                        telegramInfoEntity.HasOne(_ => _.TrialUserInfo).WithOne(_ => _.Telegram).HasPrincipalKey<Info.Entity>().IsRequired();
                    }
                }
            }
        }
    }
}
