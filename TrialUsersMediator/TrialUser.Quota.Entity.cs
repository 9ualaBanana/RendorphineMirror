using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace TrialUsersMediator;

public partial record TrialUser
{
    public partial record Quota<EQuota>
        where EQuota : struct, Enum
    {
        public partial record Entity : Quota<EQuota>
        {
            public int Id { get; private set; } = default!;
            public TrialUser.Entity TrialUser { get; private set; } = default!;
            public IEnumerable<Entry> Entries { get; private set; } = new List<Entry>();

            Entity() { }
            internal Entity(Quota<EQuota> quota)
                : base(quota)
            {
                Entries = quota._entries.Select(_ => new Entry(_.Key, _.Value) { Quota = this }).ToList();
            }

            internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
            {
                public void Configure(EntityTypeBuilder<Entity> taskQuotaEntity)
                {
                    taskQuotaEntity.ToTable("TrialUsers_Quota");

                    taskQuotaEntity.OwnsMany(_ => _.Entries, quotaEntry =>
                    {
                        quotaEntry.WithOwner(_ => _.Quota);

                        quotaEntry.ToTable("TrialUsers_QuotaEntries");
                        
                        quotaEntry.HasKey(_ => _.Id);
                    });
                }
            }
        }
    }
}
