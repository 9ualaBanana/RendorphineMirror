using Microsoft.EntityFrameworkCore;

namespace Telegram.Persistence;

public class TelegramBotDbContext : DbContext
{
    public DbSet<TelegramBotUserEntity> Users { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TelegramBotDbContext(DbContextOptions<TelegramBotDbContext> options)
        : base(options)
    {
    }

    public TelegramBotDbContext()
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("DataSource=bot.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(default(TelegramBotUserEntity.Configuration))
            .ApplyConfiguration(default(MPlusIdentityEntity.Configuration));
    }
}
