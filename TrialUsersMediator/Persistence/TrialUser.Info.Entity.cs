using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telegram.Infrastructure.Bot;

namespace TrialUsersMediator.Persistence;

public partial record TrialUser
{
    internal partial record Info
    {
        internal record Entity
        {
            internal TrialUser.Entity TrialUser { get; private set; } = default!;
            internal Info.Telegram.Entity? Telegram { get; private set; } = default!;


            internal class Wrapper
            {
                internal static Entity For(TelegramBot.User.LoginWidgetData telegramInfo)
                    => new() { Telegram = Info.Telegram.Entity.Wrapper.For(telegramInfo) };
            }


            internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
            {
                public void Configure(EntityTypeBuilder<Entity> trialUserInfoEntity)
                {
                    trialUserInfoEntity.ToTable("TrialUsers_Info");

                    const string Id = nameof(Id);
                    trialUserInfoEntity.Property<int>(Id).ValueGeneratedOnAdd();
                    trialUserInfoEntity.HasKey(Id);

                    trialUserInfoEntity.HasOne(_ => _.Telegram).WithOne(_ => _.TrialUserInfo).HasPrincipalKey<TrialUser.Info.Entity>();
                    trialUserInfoEntity.Navigation(_ => _.Telegram).AutoInclude();
                }
            }
        }
    }
}
