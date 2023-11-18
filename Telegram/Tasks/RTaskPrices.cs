using Newtonsoft.Json.Linq;
using NLog;
using ILogger = NLog.ILogger;

namespace Telegram.Tasks;

/// <summary>
/// Stores <see cref="JToken"/> that contains prices for <see cref="TaskAction"/>.
/// </summary>
internal class RTaskPrices
{
    readonly JToken _prices;

    readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    internal RTaskPrices(JToken prices)
    {
        _prices = prices;
    }

    internal double this[TaskAction rTaskAction]
    {
        get
        {
            if (_prices[rTaskAction.ToString()]?.Value<double>() is double price)
                return price / 100; // Prices are returned in euro cents so we convert them to euro.
            else
            {
                var exception = new ArgumentException($"Price for {rTaskAction} task is not available.", nameof(rTaskAction));
                _logger.Fatal(exception);
                throw exception;
            }
        }
    }
}
