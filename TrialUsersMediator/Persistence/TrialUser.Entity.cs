using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrialUsersMediator.Persistence;

public partial record TrialUser
{
    public record Entity : TrialUser
    {
        internal TaskQuota.Entity Quota_ { get; private set; } = default!;
        internal Info.Entity Info_ { get; private set; } = default!;

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

            internal Wrapper AssociatedWith(TaskQuota quota)
            { _trialUserEntity.Quota_ = TaskQuota.Entity.Wrapper.For(quota); return this; }

            internal TrialUser.Entity And(TrialUser.Info.Entity info)
            { _trialUserEntity.Info_ = info; return ValidatedProduct; }

            protected override TrialUser.Entity ValidatedProduct
            {
                get
                {
                    _ = base.ValidatedProduct;
                    ArgumentNullException.ThrowIfNull(_trialUserEntity.Quota_, nameof(_trialUserEntity.Quota_));
                    ArgumentNullException.ThrowIfNull(_trialUserEntity.Info_, nameof(_trialUserEntity.Info_));

                    return _trialUserEntity;
                }
            }
        }


        internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
        {
            public void Configure(EntityTypeBuilder<Entity> trialUserEntity)
            {
                trialUserEntity.ToTable("TrialUsers");

                trialUserEntity.HasKey(_ => new { _.Identifier, _.Platform });

                trialUserEntity.HasOne(_ => _.Quota_).WithOne().HasPrincipalKey<TrialUser.Entity>().IsRequired();
                trialUserEntity.Navigation(_ => _.Quota_).AutoInclude();

                trialUserEntity.HasOne(_ => _.Info_).WithOne(_ => _.TrialUser).HasPrincipalKey<TrialUser.Entity>();
                trialUserEntity.Navigation(_ => _.Info_).AutoInclude();
            }
        }
    }
}
