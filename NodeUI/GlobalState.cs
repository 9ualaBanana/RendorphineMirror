namespace NodeUI
{
    public static class GlobalState
    {
        public static readonly Bindable<INodeState> State = new(IdleNodeState.Instance);

        public static string GetName(this INodeState state) => state.GetType().Name[..^"NodeState".Length];
        public static void SubscribeChanged<T>(ChangedDelegate<INodeState> func) where T : INodeState
        {
            State.Changed += (oldstate, newstate) =>
            {
                if (oldstate.GetType() != typeof(T) && newstate.GetType() != typeof(T)) return;

                func(oldstate, newstate);
            };
        }
    }
}