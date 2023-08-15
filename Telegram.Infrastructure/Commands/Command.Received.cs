﻿using ILogger = NLog.ILogger;

namespace GIBS.Commands;

public partial class Command
{
    public class Received
    {
        readonly IHttpContextAccessor _httpContextAccessor;

        readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

        public Received(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        internal void Set(Command command)
            => _httpContextAccessor.HttpContext!.Items[_receivedCommandIndex] = command;

        internal Command Get()
        {
            if (_httpContextAccessor.HttpContext!.Items[_receivedCommandIndex] is Command command)
                return command;
            else
            {
                var exception = new InvalidOperationException($"{nameof(Set)} before attempting to {nameof(Get)}.");
                _logger.Fatal(exception);
                throw exception;
            }
        }

        static readonly object _receivedCommandIndex = new();
    }
}
