using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrialUsersMediator.Persistence;

public partial record TrialUser
{
    public record Entity : TrialUser
    {
        internal TaskQuota.Entity Quota_ { get; private set; } = default!;

        Entity(TrialUser trialUser)
            : base(trialUser)
        {
        }
        Entity() { }


        internal class Wrapper : TrialUser.Builder
        {
            readonly TrialUser.Entity _trialUserEntity;

            new internal static Wrapper For(TrialUser trialUser)
                => new(trialUser);
            Wrapper(TrialUser trialUser)
                : base(trialUser)
            { _trialUserEntity = new Entity(trialUser); }

            internal TrialUser.Entity With(TaskQuota quota)
            { _trialUserEntity.Quota_ = TaskQuota.Entity.Wrapper.For(quota); return ValidatedProduct; }

            protected override TrialUser.Entity ValidatedProduct
            {
                get
                {
                    _ = base.ValidatedProduct;
                    ArgumentNullException.ThrowIfNull(_trialUserEntity.Quota_, nameof(_trialUserEntity.Quota_));

                    return _trialUserEntity;
                }
            }
        }


        internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
        {
            public void Configure(EntityTypeBuilder<Entity> trialUserEntity)
            {
                trialUserEntity.HasKey(_ => new { _.Identifier, _.Platform });
                trialUserEntity.HasOne(_ => _.Quota_).WithOne().HasPrincipalKey<TrialUser.Entity>().IsRequired();
                trialUserEntity.Navigation(_ => _.Quota_).AutoInclude();
            }
        }
    }
}
