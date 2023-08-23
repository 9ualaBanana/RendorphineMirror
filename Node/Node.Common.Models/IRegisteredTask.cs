namespace Node.Common.Models;

/// <summary>
/// Task that has a unique <see cref="Id"/>.
/// </summary>
public interface IRegisteredTask
{
    public string Id { get; }
}