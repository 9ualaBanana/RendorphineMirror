using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;
using Telegram.MediaFiles.Images;

namespace Telegram.Infrastructure.MediaFiles.Images;

public class ImagesRouterMiddleware : IMessageRouter
{
    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;

    public ImagesRouterMiddleware(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient();
        _cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? default;
    }

    public bool Matches(Message message) => message.IsImageAsync(_httpClient, _cancellationToken).Result;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    { context.Request.Path += $"/{ImagesController.PathFragment}"; await next(context); }
}
