namespace Common
{
    public interface IReadOnlyBindable<T>
    {
        public event Action<T, T> Changed;
        T Value { get; }
    }
    public class Bindable<T> : IReadOnlyBindable<T>
    {
        // <old, new>
        public event Action<T, T> Changed = delegate { };

        T _Value;
        public virtual T Value { get => _Value; set => Changed(Value!, _Value = value); }
        public readonly T DefaultValue;

        public Bindable(T defaultValue = default!) => _Value = DefaultValue = defaultValue;

        public void RaiseChangedEvent() => Changed(Value!, Value!);
        public void SubscribeChanged(Action<T, T> action, bool invokeImmediately = false)
        {
            Changed += action;
            if (invokeImmediately) action(Value, Value);
        }
        public void Reset() => Value = DefaultValue;

        public void SetValue(T value)
        {
            if (!EqualityComparer<T>.Default.Equals(Value, value))
                Value = value;
        }
    }
    public class IntBindable : Bindable<int>
    {
        public override int Value
        {
            get => base.Value;
            set => base.Value = Math.Clamp(value, Min, Max);
        }
        readonly int Min, Max;

        public IntBindable(int min = int.MinValue, int max = int.MaxValue, int defaultValue = default) : base(defaultValue)
        {
            Min = min;
            Max = max;
        }
    }
    public class LockableBindable<T> : Bindable<T>
    {
        public bool Locked = false;

        public override T Value
        {
            get => base.Value;
            set
            {
                if (Locked) return;
                base.Value = value;
            }
        }
    }
}