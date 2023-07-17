using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram.MPlus.Security;
using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure.Persistence;

/// <summary>
/// Entity representing <see cref="MPlusIdentity"/> of the user represented by <see cref="TelegramBot.User.Entity"/>.
/// </summary>
public record MPlusIdentityEntity : MPlusIdentity
{
    public TelegramBot.User.Entity TelegramBotUser { get; set; } = null!;
    /// <remarks>
    /// Expicitly defined Foreign Key which is also Primary Key
    /// because one can be logged in only as one user at a time,
    /// there will always be only one record of <see cref="MPlusIdentityEntity"/>
    /// in the database with a given <see cref="TelegramBotUserChatId"/> as records
    /// of logged out users are removed.
    /// </remarks>
    public ChatId TelegramBotUserChatId { set; get; } = null!;

    internal MPlusIdentityEntity(MPlusIdentity mPlusIdentity)
        : base(mPlusIdentity)
    {
    }

    internal MPlusIdentityEntity(string userId, string sessionId, AccessLevel accessLevel)
        : base(userId, sessionId, accessLevel)
    {
    }


    internal readonly struct Configuration : IEntityTypeConfiguration<MPlusIdentityEntity>
    {
        public void Configure(EntityTypeBuilder<MPlusIdentityEntity> entity)
        {
            entity.HasKey(_ => _.TelegramBotUserChatId);
            entity.HasOne(_ => _.TelegramBotUser).WithOne(_ => _.MPlusIdentity)
                .HasForeignKey<MPlusIdentityEntity>(_ => _.TelegramBotUserChatId);
        }
    }
}
