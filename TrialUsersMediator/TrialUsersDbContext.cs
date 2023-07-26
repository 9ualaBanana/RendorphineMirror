using Microsoft.EntityFrameworkCore;
using Node.Tasks.Models;

namespace TrialUsersMediator;

public class TrialUsersDbContext : DbContext
{
    public DbSet<TrialUser.Entity> AuthenticatedUsers { get; set; }
    internal async Task<TrialUser.Entity?> Authenticated(TrialUser user)
        => await AuthenticatedUsers.SingleOrDefaultAsync(trialUser => trialUser == new TrialUser.Entity(user));

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
            .ApplyConfiguration(default(TrialUser.Quota<TaskAction>.Entity.Configuration))
            .ApplyConfiguration(default(TrialUser.Info.Telegram.Entity.Configuration));
    }
}
