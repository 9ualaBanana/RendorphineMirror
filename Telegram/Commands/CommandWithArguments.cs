using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Telegram.Commands;

//internal class CommandWithArguments : Command
//{
//    internal readonly ImmutableArray<string> Arguments;

//    internal static bool TryParse(string commandMessage, [NotNullWhen(true)] out CommandWithArguments? commandWithArguments)
//    {
//        var commandTokens = new CommandTokenizer(commandMessage).Tokenize();
//        if (commandTokens.Tok)

//            commandWithArguments = new(command, arguments);
//            return true;
//        }
//        else
//        { commandWithArguments = null; return false; }
//    }

//    CommandWithArguments(string command, IEnumerable<string> arguments) : base(command)
//    {
//        Arguments = arguments.ToImmutableArray();
//    }
//}
