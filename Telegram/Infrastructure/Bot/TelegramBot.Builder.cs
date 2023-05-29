using Telegram.Infrastructure.Bot.MessagePagination;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Middleware.UpdateRouting;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;
using Telegram.Infrastructure.Persistence;
using Telegram.Security.Authentication;

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

        internal static ITelegramBotBuilder Default(IServiceCollection services)
        {
            var builder = new Builder(services)
                .ConfigureOptions()
                .AddUpdateRouting()
                .AddCommandsCore()
                .AddCallbackQueries()
                .AddMessageRouter<UnspecificMessageRouterMiddleware>()
                .AddMessagePagination()
                .AddPersistence();

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
