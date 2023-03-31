﻿using System.Text.RegularExpressions;
using Telegram.Infrastructure.LinguisticAnalysis;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

internal class QuotedCommandArgumentLexemeScanner : LexemeScanner
{
    internal static LexemeScanner Instance = new QuotedCommandArgumentLexemeScanner();

    internal override Regex Pattern => new("^\".*?\"", RegexOptions.Compiled);

    protected override Token Token(string lexeme) => new QuotedCommandArgumentToken(lexeme);
}

internal class QuotedCommandArgumentToken : Token
{
    internal QuotedCommandArgumentToken(string lexeme) : base(lexeme)
    {
    }

    protected override string Evaluate(string lexeme) => lexeme.Trim('"');
}
