using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.MediaFiles.Images;

public class ImagesRouterMiddleware : MessageRouter
{
    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;

    protected override string PathFragment => ImagesController.PathFragment;

    public ImagesRouterMiddleware(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient();
        _cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? default;
    }

    public override bool Matches(Message message) => message.IsImageAsync(_httpClient, _cancellationToken).Result;
}
