using Telegram.Bot.Types;

namespace Telegram.Services.Telegram.Updates.Images;

public record TelegramImage(int? Size, string FileId)
{
    public static TelegramImage From(PhotoSize photo) => new(photo.FileSize, photo.FileId);
    public static TelegramImage From(Document document) => new(document.FileSize, document.FileId);
}
