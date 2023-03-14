using Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis;

internal static class CommandsLexicalAnalysisExtensions
{
    internal static IServiceCollection AddCommandsTokenization(this IServiceCollection services)
        => services
        .AddScoped<CommandTokenizer>()
        .AddScoped<LexemeScanner, CommandLexemeScanner>()
        .AddScoped<LexemeScanner, UnquotedCommandArgumentLexemeScanner>()
        .AddScoped<LexemeScanner, QuotedCommandArgumentLexemeScanner>()
        .AddScoped<LexemeScanner, WhitespaceLexemeScanner>()
        .AddScoped<LexemeScanner, InvalidLexemeScanner>();
}
