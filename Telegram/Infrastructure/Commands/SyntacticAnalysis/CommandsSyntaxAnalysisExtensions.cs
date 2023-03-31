using Telegram.Infrastructure.Commands.LexicalAnalysis;

namespace Telegram.Infrastructure.Commands.SyntacticAnalysis;

internal static class CommandsSyntaxAnalysisExtensions
{
    internal static IServiceCollection AddCommandParsing(this IServiceCollection services)
        => services
        .AddCommandsTokenization()
        .AddScoped<CommandParser>();
}
