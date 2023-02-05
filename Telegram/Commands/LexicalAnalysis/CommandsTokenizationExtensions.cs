using Telegram.Commands.Tokenization.Tokens;

namespace Telegram.Commands.Tokenization;

internal static class CommandsTokenizationExtensions
{
    internal static IServiceCollection AddCommandsTokenization(this IServiceCollection services)
        => services
        .AddScoped<CommandTokenizer>()
        .AddScoped<CommandLexemeScanner, CommandLexemeScanner>()
        .AddScoped<LexemeScanner, UnquotedCommandArgumentLexemeScanner>()
        .AddScoped<LexemeScanner, QuotedCommandArgumentLexemeScanner>()
        .AddScoped<LexemeScanner, WhitespaceLexemeScanner>()
        .AddScoped<LexemeScanner, InvalidLexemeScanner>();
}
