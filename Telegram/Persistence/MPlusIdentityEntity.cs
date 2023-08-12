using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram.MPlus.Security;

namespace Telegram.Persistence;

/// <summary>
/// Entity representing <see cref="MPlusIdentity"/> of the user represented by <see cref="TelegramBot.User.Entity"/>.
/// </summary>
public record MPlusIdentityEntity : MPlusIdentity
{
    /// <remarks>
    /// Expicitly defined Foreign Key which is also Primary Key
    /// because one can be logged in only as one user at a time,
    /// there will always be only one record of <see cref="MPlusIdentityEntity"/>
    /// in the database with a given <see cref="UserChatId"/> as records
    /// of logged out users are removed.
    /// </remarks>
    public ChatId UserChatId { get; private init; } = default!;
    public TelegramBotUserEntity TelegramBotUser { get; private init; } = default!;

    internal MPlusIdentityEntity(MPlusIdentity mPlusIdentity)
        : base(mPlusIdentity)
    {
    }

    internal MPlusIdentityEntity(string email, string userId, string sessionId, AccessLevel accessLevel)
        : base(email, userId, sessionId, accessLevel)
    {
    }


    internal readonly struct Configuration : IEntityTypeConfiguration<MPlusIdentityEntity>
    {
        public void Configure(EntityTypeBuilder<MPlusIdentityEntity> mPlusIdentityEntity)
        {
            mPlusIdentityEntity.ToTable("MPIdentity");

            mPlusIdentityEntity.HasKey(_ => _.UserChatId);

            mPlusIdentityEntity.HasOne(_ => _.TelegramBotUser).WithOne(_ => _.MPlusIdentity)
                .HasForeignKey<MPlusIdentityEntity>(_ => _.UserChatId);
        }
    }
}
