using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static _3DProductsPublish.Turbosquid.Upload.TurboSquid.SaleReports_;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial class SaleReports_
    {
        public class DbContext : Microsoft.EntityFrameworkCore.DbContext
        {
            internal void Add(TurboSquid.ScanTimePeriodEntity _)
            { _.User.ScanPeriods.Add(_); Update(_.User); SaveChanges(); }
            public DbSet<TurboSquid.UserEntity> TurboSquid { get; set; }

            public DbContext(DbContextOptions<DbContext> options)
                : base(options)
            {
            }
            public DbContext()
            {
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            { optionsBuilder.UseSqlite("DataSource=salesreports.db"); }

            protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder
                .ApplyConfiguration(default(TurboSquid.UserEntity.Configuration));
        }
    }


    public record UserEntity(string Email)
    {
        public List<ScanTimePeriodEntity> ScanPeriods { get; private init; } = [];

        internal readonly struct Configuration : IEntityTypeConfiguration<UserEntity>
        {
            public void Configure(EntityTypeBuilder<UserEntity> _)
            {
                _.ToTable("TS_User");

                _.HasKey(_ => _.Email).HasName(nameof(Email));

                var scanPeriod = _.OwnsMany(_ => _.ScanPeriods)
                    .ToTable("TS_ScanPeriod");
                scanPeriod.HasKey(_ => new { _.Start, _.End });
                scanPeriod.Property(_ => _.IsAnalyzed).HasDefaultValue(false);
            }
        }
    }
    public record ScanTimePeriodEntity : MonthlyScan.TimePeriod_
    {
        public UserEntity User { get; internal set; } = default!;
        public bool IsAnalyzed { get; internal set; } = default!;

        public ScanTimePeriodEntity(long start, long end)
            : base(start, end)
        {
        }
        public ScanTimePeriodEntity(MonthlyScan.TimePeriod_ original)
            : base(original)
        {
        }
    }
}
