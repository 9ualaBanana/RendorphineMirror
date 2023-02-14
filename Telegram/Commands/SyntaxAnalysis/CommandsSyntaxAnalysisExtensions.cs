using Telegram.Commands.Tokenization;

namespace Telegram.Commands.SyntaxAnalysis;

internal static class CommandsSyntaxAnalysisExtensions
{
    internal static IServiceCollection AddCommandsParsing(this IServiceCollection services)
        => services
        .AddCommandsTokenization()
        .AddScoped<CommandParser>();
}
