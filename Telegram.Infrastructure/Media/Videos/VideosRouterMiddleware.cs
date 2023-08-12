using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Media.Videos;

public class VideosRouterMiddleware : MessageRouter
{
    protected override string PathFragment => VideosController.PathFragment;

    public override bool Matches(Message message) => message.IsVideo();
}
