using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Persistence;

namespace Telegram.Infrastructure.Bot;

public partial class TelegramBot
{
    public partial class User
    {
        /// <summary>
        /// Principal entity with which other entities representing information related to this <see cref="ChatId"/> are associated.
        /// </summary>
        public record Entity(ChatId ChatId)
        {
            [MemberNotNullWhen(true, nameof(MPlusIdentity))]
            internal bool IsAuthenticatedByMPlus => MPlusIdentity is not null;

            public MPlusIdentityEntity? MPlusIdentity { get; set; } = default!;


            internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
            {
                public void Configure(EntityTypeBuilder<Entity> telegramBotUserEntity)
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
    }
}
