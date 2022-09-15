using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram.Telegram.Authentication.Models;

namespace Telegram.Telegram.Authentication.Services;

public sealed class AuthenticatedUsersDbContext : DbContext
{
    public DbSet<ChatAuthenticationToken> Users { get; set; } = default!;


    public AuthenticatedUsersDbContext()
    {
    }

    public AuthenticatedUsersDbContext(DbContextOptions<AuthenticatedUsersDbContext> options)
        : base(options)
    {
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite("DataSource=authenticated_users.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatAuthenticationToken>(b =>
        {
            b.Navigation(e => e.MPlus).AutoInclude();

            b.HasKey(e => e.ChatId);
            b.Property(e => e.ChatId)
             .HasConversion(p => p.Identifier, id => new ChatId((long)id!))
             .ValueGeneratedNever();
        });

        modelBuilder.Entity<MPlusAuthenticationToken>(b =>
        {
            b.HasKey(e => e.SessionId);
            b.Property(e => e.SessionId).ValueGeneratedNever();
        });
    }
}
