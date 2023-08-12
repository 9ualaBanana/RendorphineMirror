using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Messages;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Telegram.Infrastructure.Media;

public abstract class MediaHandler_ : MessageHandler_
{
    protected readonly MediaFilesCache Cache;
    protected readonly MediaFile.Factory MediaFile;
    protected readonly CallbackQuerySerializer CallbackQuery;

    protected MediaHandler_(
        MediaFilesCache cache,
        MediaFile.Factory factory,
        CallbackQuerySerializer callbackQuerySerializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(bot, httpContextAccessor, logger)
    {
        Cache = cache;
        MediaFile = factory;
        CallbackQuery = callbackQuerySerializer;
    }
}
