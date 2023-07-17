using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Persistence;

namespace Telegram.Infrastructure.Bot;

public partial class TelegramBot
{
    public partial record User
    {
        /// <summary>
        /// Principal entity with which other entities representing information related to this <see cref="ChatId"/> are associated.
        /// </summary>
        public record Entity(ChatId ChatId)
        {
            [MemberNotNullWhen(true, nameof(MPlusIdentity))]
            internal bool IsAuthenticatedByMPlus => MPlusIdentity is not null;

            public MPlusIdentityEntity? MPlusIdentity { get; set; }


            internal readonly struct Configuration : IEntityTypeConfiguration<Entity>
            {
                public void Configure(EntityTypeBuilder<Entity> entity)
                {
                    entity.HasKey(_ => _.ChatId);
                    entity.Property(_ => _.ChatId)
                        .HasConversion(chatId => chatId.Identifier, identifier => new ChatId((long)identifier!))
                        .ValueGeneratedNever();
                    entity.Navigation(_ => _.MPlusIdentity).AutoInclude();
                }
            }
        }
    }
}
