namespace Node
{
    public static class GlobalState
    {
        public static INodeState State { get; private set; } = IdleNodeState.Instance;

        /// <summary> Sets <see cref="State"/> </summary>
        /// <returns> Object that will restore the previous state when disposed </returns>
        public static TemporaryState TempSetState(INodeState state)
        {
            var prevstate = State;
            State = state;

            return new(prevstate);
        }


        public readonly struct TemporaryState : IDisposable
        {
            readonly INodeState SavedState;

            public TemporaryState(INodeState previousState) => SavedState = previousState;

            public void Dispose() => State = SavedState;
        }
    }
}