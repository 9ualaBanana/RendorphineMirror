namespace Node.Common.Models;

public interface IRegisteredTaskApi : IRegisteredTask
{
    string? HostShard { get; set; }
}