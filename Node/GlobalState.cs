namespace Node
{
    public static class GlobalState
    {
        public static readonly Bindable<INodeState> BState = new(IdleNodeState.Instance);
        public static INodeState State { get => BState.Value; set => BState.Value = value; }

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