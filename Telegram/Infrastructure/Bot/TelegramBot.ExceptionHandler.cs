using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using NLog;
using ILogger = NLog.ILogger;

namespace Telegram.Infrastructure.Bot;

static partial class TelegramBotExceptionHandler
{
    readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

    internal static ITelegramBotBuilder ConfigureExceptionHandlerOptions(this ITelegramBotBuilder builder)
    {
        builder.Services.AddOptions<Options>()
            .BindConfiguration($"{TelegramBot.Options.Configuration}:{Options.Configuration}");
        return builder;
    }


    internal static IApplicationBuilder UseTelegramBotExceptionHandler(this IApplicationBuilder app)
        => app.UseExceptionHandler(_ => _.Run(async context =>
        {
            await TelegramBotExceptionHandler.InvokeAsync(context);

            // We tell Telegram the Update is handled.
            context.Response.StatusCode = 200;
            await context.Response.StartAsync();
        }));

    internal static async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var exception = context.Features.GetRequiredFeature<IExceptionHandlerFeature>();
            var exceptionDetails =
                $"{exception.Error.Message}\n" +
                $"{exception.Error.StackTrace?.Replace(@"\", @"\\").Replace("`", @"\`")}";

            var bot = context.RequestServices.GetRequiredService<TelegramBot>();
            var subscribers = context.RequestServices.GetRequiredService<IOptionsSnapshot<Options>>().Value.Subscribers;

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
