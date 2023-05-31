using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands.LexicalAnalysis;

namespace Telegram.Infrastructure.Commands.SyntacticAnalysis;

internal static class CommandsSyntacticAnalysisExtensions
{
    internal static ITelegramBotBuilder AddCommandsParsing(this ITelegramBotBuilder builder)
    {
        builder.AddCommandsTokenization();
        builder.Services.TryAddScoped<Command.Parser>();

        return builder;
    }
}
