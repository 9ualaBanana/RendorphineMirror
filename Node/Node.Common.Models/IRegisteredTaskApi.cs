namespace Node.Common.Models;

public interface IRegisteredTaskApi : IRegisteredTask, ILoggable
{
    string? HostShard { get; set; }
}