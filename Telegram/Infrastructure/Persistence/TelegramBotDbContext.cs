using Microsoft.EntityFrameworkCore;
using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure.Persistence;

public class TelegramBotDbContext : DbContext
{
    public DbSet<TelegramBot.User.Entity> Users { get; set; }

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
            .ApplyConfiguration(default(TelegramBot.User.Entity.Configuration))
            .ApplyConfiguration(default(MPlusIdentityEntity.Configuration));
    }
}
