using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

            Entity() { }
            internal Entity(Quota<EQuota> quota)
                : base(quota)
            {
            }
            

            internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
            {
                public void Configure(EntityTypeBuilder<Entity> taskQuotaEntity)
                {
                    taskQuotaEntity.ToTable("TrialUsers_Quota");

                    taskQuotaEntity.Property(_ => _._entries)
                        .HasColumnName("Entries")
                        .HasConversion(
                        _ => JsonConvert.SerializeObject(_),
                        _ => JsonConvert.DeserializeObject<Dictionary<EQuota, int>>(_)!,
                        new ValueComparer<Dictionary<EQuota, int>>(
                            (this_, that) => JsonConvert.SerializeObject(this_) == JsonConvert.SerializeObject(that),
                            _ => _.GetHashCode())
                        );
                }
            }
        }
    }
}
