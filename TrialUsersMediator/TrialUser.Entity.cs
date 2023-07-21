using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Node.Tasks.Models;
using System.Diagnostics.CodeAnalysis;

namespace TrialUsersMediator;

public partial record TrialUser
{
    public record Entity : TrialUser
    {
        required public Quota<TaskAction>.Entity Quota_ { get; set; } = default!;
        required public Info.Entity Info_ { get; set; } = default!;

        Entity() { }
        [SetsRequiredMembers]
        internal Entity(TrialUser trialUser)
            : base(trialUser)
        {
        }

        internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
        {
            public void Configure(EntityTypeBuilder<Entity> trialUserEntity)
            {
                trialUserEntity.ToTable("TrialUsers");

                trialUserEntity.HasKey(_ => new { _.Identifier, _.Platform });

                trialUserEntity.HasOne(_ => _.Quota_).WithOne(_ => _.TrialUser)
                    .HasPrincipalKey<TrialUser.Entity>()
                    .IsRequired();
                trialUserEntity.Navigation(_ => _.Quota_).AutoInclude();

                trialUserEntity.OwnsOne(_ => _.Info_, trialUserInfo =>
                {
                    trialUserInfo.WithOwner(_ => _.TrialUser);

                    trialUserInfo.ToTable("TrialUsers_Info");

                    trialUserInfo.HasOne(_ => _.Telegram).WithOne()
                        .HasForeignKey<TrialUser.Info.Entity>("TelegramInfoId");
                    trialUserInfo.Navigation(_ => _.Telegram).AutoInclude();
                });
            }
        }
    }
}
