using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;
using Telegram.Infrastructure.Tokenization;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis;

static class CommandsLexicalAnalysisExtensions
{
    internal static ITelegramBotBuilder AddCommandsTokenization(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped<Tokenizer<CommandToken_>>();
        builder.Services.TryAddScoped_<LexemeScanner<CommandToken_>, CommandToken.LexemeScanner>();
        builder.Services.TryAddScoped_<LexemeScanner<CommandToken_>, QuotedCommandArgumentToken.LexemeScanner>();
        builder.Services.TryAddScoped_<LexemeScanner<CommandToken_>, UnquotedCommandArgumentToken.LexemeScanner>();
        builder.Services.TryAddScoped_<LexemeScanner<CommandToken_>, WhitespaceToken.LexemeScanner>();
        builder.Services.TryAddScoped_<LexemeScanner<CommandToken_>, InvalidToken.LexemeScanner>();

        return builder;
    }
        
}
