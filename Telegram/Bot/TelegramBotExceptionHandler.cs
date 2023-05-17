using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using NLog;
using Telegram.Bot.Types;
using ILogger = NLog.ILogger;

namespace Telegram.Bot;

/// <param name="Subscribers"><see cref="ChatId"/>s of users that should be notified about unhandled exceptions.</param>
public record TelegramBotExceptionHandlerOptions(HashSet<long> Subscribers)
{
    internal const string Configuration = "Exceptions";

    public TelegramBotExceptionHandlerOptions()
        : this((HashSet<long>)null!)
    {
    }
}

static class TelegramBotExceptionHandler
{
    readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

    internal static IServiceCollection ConfigureTelegramBotExceptionHandlerOptions(this IServiceCollection services)
        => services.AddOptions<TelegramBotExceptionHandlerOptions>()
        .BindConfiguration($"{TelegramBotOptions.Configuration}:{TelegramBotExceptionHandlerOptions.Configuration}")
        .Services;

    internal static async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var exception = context.Features.GetRequiredFeature<IExceptionHandlerFeature>();
            var exceptionDetails =
                $"{exception.Error.Message}\n" +
                $"{exception.Error.StackTrace?.Replace(@"\", @"\\").Replace("`", @"\`")}";

            var bot = context.RequestServices.GetRequiredService<TelegramBot>();
            var subscribers = context.RequestServices.GetRequiredService<IOptionsSnapshot<TelegramBotExceptionHandlerOptions>>().Value.Subscribers;

            foreach (var subscriber in subscribers)
                await bot.SendMessageAsync_(subscriber,
                    $"`{exception.Path}` handler thrown an unhandled exception.\n\n{exceptionDetails}",
                    disableNotification: true,
                    cancellationToken: context.RequestAborted);
        }
        catch (Exception ex)
        { _logger.Error(ex, $"{nameof(TelegramBotExceptionHandler)} must not throw"); }
    }
}
