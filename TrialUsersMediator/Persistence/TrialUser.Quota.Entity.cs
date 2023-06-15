using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TrialUsersMediator.Persistence;

public partial record TrialUser
{
    public partial record Quota<EQuota>
        where EQuota : struct, Enum
    {
        public partial record Entity : Quota<EQuota>
        {
            internal Entry.Collection Entries { get; private set; } = default!;
            
            Entity(Quota<EQuota> quota)
                : base(quota)
            { Entries = Entry.Collection.From(_entries).OwnedBy(this); }
            Entity() { }


            internal class Wrapper
            {
                internal static Entity For(Quota<EQuota> quota)
                    => new(quota);
            }


            internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
            {
                public void Configure(EntityTypeBuilder<Entity> quotaEntity)
                {
                    const string Id = nameof(Id);
                    quotaEntity.Property<int>(Id).ValueGeneratedOnAdd();
                    quotaEntity.HasKey(Id);

                    quotaEntity.OwnsMany(_ => _.Entries, quotaEntry =>
                    {
                        quotaEntry.WithOwner(_ => _.Quota);

                        quotaEntry.Property<int>(Id).ValueGeneratedOnAdd();
                        quotaEntry.HasKey(Id);

                        quotaEntry.ToTable("QuotaEntries");
                    });
                    quotaEntity.ToTable("Quotas");
                }
            }
        }
    }
}
