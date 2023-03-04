using Telegram.Commands.LexicalAnalysis;

namespace Telegram.Commands.SyntacticAnalysis;

internal static class CommandsSyntaxAnalysisExtensions
{
    internal static IServiceCollection AddCommandParsing(this IServiceCollection services)
        => services
        .AddCommandsTokenization()
        .AddScoped<CommandParser>();
}
