using Telegram.Infrastructure.Commands.LexicalAnalysis;

namespace Telegram.Infrastructure.Commands.SyntacticAnalysis;

internal static class CommandsSyntacticAnalysisExtensions
{
    internal static IServiceCollection AddCommandsParsing(this IServiceCollection services)
        => services
        .AddScoped<Command.Parser>()
        .AddCommandsTokenization();
}
