using Telegram.Commands.Tokenization;

namespace Telegram.Commands.SyntaxAnalysis;

internal static class CommandsSyntaxAnalysisExtensions
{
    internal static IServiceCollection AddCommandParsing(this IServiceCollection services)
        => services
        .AddCommandsTokenization()
        .AddScoped<CommandParser>();
}
