using Node.Profiling;

namespace Node.DataStorage;

public interface INodeLoadStorage
{
    DatabaseAccessor<long, NodeLoad> NodeFullLoad { get; }
    Database LoadDatabase { get; }
}
