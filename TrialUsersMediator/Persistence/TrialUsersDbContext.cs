using Microsoft.EntityFrameworkCore;

namespace TrialUsersMediator.Persistence;

public class TrialUsersDbContext : DbContext
{
    public DbSet<TrialUser.Entity> AuthenticatedUsers { get; set; }

    public TrialUsersDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected TrialUsersDbContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("DataSource=trialusers.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(default(TrialUser.Entity.Configuration))
            .ApplyConfiguration(default(TrialUser.TaskQuota.Entity.Configuration))
            .ApplyConfiguration(default(TrialUser.Info.Entity.Configuration))
            .ApplyConfiguration(default(TrialUser.Info.Telegram.Entity.Configuration));
    }
}
