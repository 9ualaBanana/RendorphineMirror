using Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;
using Telegram.Infrastructure.Tokenization;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis;

internal static class CommandsLexicalAnalysisExtensions
{
    internal static IServiceCollection AddCommandsTokenization(this IServiceCollection services)
        => services
        .AddScoped<Tokenizer<CommandToken_>>()
        .AddScoped<LexemeScanner<CommandToken_>, CommandToken.LexemeScanner>()
        .AddScoped<LexemeScanner<CommandToken_>, QuotedCommandArgumentToken.LexemeScanner>()
        .AddScoped<LexemeScanner<CommandToken_>, UnquotedCommandArgumentToken.LexemeScanner>()
        .AddScoped<LexemeScanner<CommandToken_>, WhitespaceToken.LexemeScanner>()
        .AddScoped<LexemeScanner<CommandToken_>, InvalidToken.LexemeScanner>();
}
