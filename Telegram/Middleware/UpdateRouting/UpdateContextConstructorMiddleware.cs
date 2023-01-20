using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot.Types;
using Telegram.Models;

namespace Telegram.Middleware.UpdateRouting;

public class UpdateContextConstructorMiddleware : IMiddleware
{
    readonly ILogger _logger;

    readonly UpdateContextCache _updateContextCache;

    public UpdateContextConstructorMiddleware(
        UpdateContextCache updateContextCache,
        ILogger<UpdateContextConstructorMiddleware> logger)
    {
        _updateContextCache = updateContextCache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var update = await DeserializeUpdateAsyncFrom(context);
        EnsureUpdateIsSuccessfullyDeserialized(update);

        _updateContextCache.Cache(new UpdateContext(update));
        _logger.LogTrace($"{nameof(UpdateContext)} is constructed");

        await next(context);
    }

    static async Task<Update?> DeserializeUpdateAsyncFrom(HttpContext context)
    {
        // TODO: Remove after refactoring. Added temporary to allow non-refactored action method perform model binding.
        context.Request.EnableBuffering();
        // We have to create a buffer stream because request body must be read asynchronously which is not supported by Newtonsoft.Json (like WTF fr?)
        // Task.Run() doesn't help in this case as it doesn't make deserialization truly async.
        using var bufferStream = new MemoryStream();
        await context.Request.Body.CopyToAsync(bufferStream, context.RequestAborted);
        bufferStream.Position = 0;
        // TODO: Remove after refactoring. Added temporary to allow non-refactored action method perform model binding.
        context.Request.Body.Position = 0;
        using var streamReader = new StreamReader(bufferStream);
        using var jsonReader = new JsonTextReader(streamReader);
        return await Task.Run(() => JsonSerializer.CreateDefault().Deserialize<Update>(jsonReader));
    }

    void EnsureUpdateIsSuccessfullyDeserialized([NotNull] Update? update)
    {
        if (update is null)
        {
            string errorMessage = $"Request body didn't contain JSON representing {nameof(Update)}";
            _logger.LogCritical(errorMessage);
            throw new InvalidDataException(errorMessage);
        }
    }
}
