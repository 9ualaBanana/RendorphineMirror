using Newtonsoft.Json.Linq;
using NLog;
using ILogger = NLog.ILogger;

namespace Telegram.Tasks;

/// <summary>
/// Stores <see cref="JToken"/> that contains prices for <see cref="TaskAction"/>.
/// </summary>
internal class TaskPrices
{
    readonly JToken _prices;

    readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    internal TaskPrices(JToken prices)
    {
        _prices = prices;
    }

    internal double this[TaskAction action]
    {
        get
        {
            if (_prices[action.ToString()]?.Value<double>() is double price)
                return price / 100; // Prices are returned in euro cents so we convert them to euro.
            else
            {
                var exception = new ArgumentException($"Price for {action} task is not available.", nameof(action));
                _logger.Fatal(exception);
                throw exception;
            }
        }
    }
}
