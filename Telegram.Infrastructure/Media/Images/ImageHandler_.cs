using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.MediaFiles;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Telegram.Infrastructure.Media.Images;

public abstract class ImageHandler_ : MediaHandler_
{
    protected ImageHandler_(
        MediaFilesCache cache,
        MediaFile.Factory factory,
        CallbackQuerySerializer callbackQuerySerializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(cache, factory, callbackQuerySerializer, bot, httpContextAccessor, logger)
    {
    }
}
