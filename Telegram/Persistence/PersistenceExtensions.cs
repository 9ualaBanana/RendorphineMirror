namespace Telegram.Persistence;

static class PersistenceExtensions
{
    internal static ITelegramBotBuilder AddPersistence(this ITelegramBotBuilder builder)
    {
        builder.Services.AddDbContext<TelegramBotDbContext>();

        return builder;
    }
}
