namespace Common.NodeToUI
{
    public interface INodeState { }
    public class IdleNodeState : INodeState
    {
        public static readonly INodeState Instance = new IdleNodeState();

        private IdleNodeState() { }
    }
    public class BenchmarkNodeState : INodeState
    {
        public readonly List<string> Completed = new();
    }

    // TODO:
    public class ExecutingTaskNodeState : INodeState
    {
        public ExecutingTaskNodeState() { }
    }
}