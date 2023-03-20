﻿using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Telegram.Persistence;

public class TelegramBotDbContext : DbContext
{
    public DbSet<TelegramBotUserEntity> Users { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TelegramBotDbContext()
    {
    }

    public TelegramBotDbContext(DbContextOptions<TelegramBotDbContext> options) : base(options)
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(@"DataSource=bot.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(new TelegramBotUserEntityConfiguration())
            .ApplyConfiguration(new MPlusIdentityEntityConfiguration());
    }
}

#region EntityTypeConfiguration

class TelegramBotUserEntityConfiguration : IEntityTypeConfiguration<TelegramBotUserEntity>
{
    public void Configure(EntityTypeBuilder<TelegramBotUserEntity> entity)
    {
        entity.HasKey(u => u.ChatId);
        entity.Property(u => u.ChatId)
            .HasConversion(chatId => chatId.Identifier, identifier => new ChatId((long)identifier!))
            .ValueGeneratedNever();
        entity.Navigation(u => u.MPlusIdentity).AutoInclude();
    }
}

class MPlusIdentityEntityConfiguration : IEntityTypeConfiguration<MPlusIdentityEntity>
{
    public void Configure(EntityTypeBuilder<MPlusIdentityEntity> entity)
    {
        entity.HasKey(i => i.UserId);
        entity.HasOne(i => i.TelegramBotUser).WithOne(u => u.MPlusIdentity).HasForeignKey<MPlusIdentityEntity>();
    }
}

#endregion
