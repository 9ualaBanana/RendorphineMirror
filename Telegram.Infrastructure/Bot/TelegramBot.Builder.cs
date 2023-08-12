using Telegram.Infrastructure.Bot.MessagePagination;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Media.Images;
using Telegram.Infrastructure.Media.Videos;
using Telegram.Infrastructure.Messages;
using Telegram.Infrastructure.Middleware.UpdateRouting;

namespace Telegram.Infrastructure.Bot;

public interface ITelegramBotBuilder
{
    IServiceCollection Services { get; }
}

public partial class TelegramBot
{
    internal class Builder : ITelegramBotBuilder
    {
        public IServiceCollection Services { get; }

        internal static ITelegramBotBuilder Default(IServiceCollection services, Action<ITelegramBotBuilder> configure)
        {
            var builder = new Builder(services);
            configure(builder); // Must be called here because otherwise Update handlers order becomes fucked up.

            builder
                .ConfigureOptions()
                .AddUpdateRouting()
                .AddCallbackQueriesCore()
                .AddCommandsCore()
                .AddImagesCore()
                .AddVideosCore()
                .AddMessagesCore()
                .AddMessagePagination();

            builder.Services
                .AddSingleton<TelegramBot>()
                .AddHttpClient()
                // Telegram.Bot works only with Newtonsoft.
                .AddControllers().AddNewtonsoftJson();
            return builder;
        }
            

        internal Builder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
