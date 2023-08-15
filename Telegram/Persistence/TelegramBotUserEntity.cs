using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Telegram.Persistence;

/// <summary>
/// Principal entity with which other entities representing information related to this <see cref="ChatId"/> are associated.
/// </summary>
public record TelegramBotUserEntity(ChatId ChatId)
{
    [MemberNotNullWhen(true, nameof(MPlusIdentity))]
    internal bool IsAuthenticatedByMPlus => MPlusIdentity is not null;

    public MPlusIdentityEntity? MPlusIdentity { get; set; } = default!;


    internal readonly struct Configuration : IEntityTypeConfiguration<TelegramBotUserEntity>
    {
        public void Configure(EntityTypeBuilder<TelegramBotUserEntity> telegramBotUserEntity)
        {
            telegramBotUserEntity.ToTable("Users");

            telegramBotUserEntity.HasKey(_ => _.ChatId);

            telegramBotUserEntity.Property(_ => _.ChatId)
                .HasConversion(chatId => chatId.Identifier, identifier => new ChatId((long)identifier!))
                .ValueGeneratedNever();

            telegramBotUserEntity.Navigation(_ => _.MPlusIdentity).AutoInclude();
        }
    }
}
