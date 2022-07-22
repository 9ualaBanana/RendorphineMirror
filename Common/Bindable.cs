namespace Common
{
    public delegate void ChangedDelegate<T>(T oldv, T newv);

    public interface IBindable { }
    public interface IReadOnlyBindable<T> : IBindable
    {
        public event ChangedDelegate<T> Changed;
        T Value { get; }
    }
    public class Bindable<T> : IReadOnlyBindable<T>
    {
        public event ChangedDelegate<T> Changed = delegate { };
        List<WeakReference<Bindable<T>>>? Bounds;

        protected T _Value;
        public virtual T Value
        {
            get => _Value;
            set
            {
                var oldvalue = _Value;
                _Value = value;

                if (!EqualityComparer<T>.Default.Equals(value, oldvalue))
                    Changed(oldvalue, value);
            }
        }
        public readonly T DefaultValue;

        public Bindable(T defaultValue = default!)
        {
            _Value = DefaultValue = defaultValue;
            Changed += updateBound;


            void updateBound(T _, T __)
            {
                if (Bounds is null) return;

                for (int i = 0; i < Bounds.Count; i++)
                {
                    var weakr = Bounds[i];
                    if (!weakr.TryGetTarget(out var b))
                    {
                        i--;
                        Bounds.Remove(weakr);
                        continue;
                    }

                    b.Value = Value;
                }
            }
        }

        public void RaiseChangedEvent() => Changed(Value!, Value!);
        public void SubscribeChanged(ChangedDelegate<T> action, bool invokeImmediately = false)
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

        public void Bound(Bindable<T> bindable)
        {
            (Bounds ??= new()).Add(new(bindable));
            (bindable.Bounds ??= new()).Add(new(this));
        }
        public Bindable<T> GetBoundCopy()
        {
            var b = new Bindable<T>(Value);
            b.Bound(this);

            return b;
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

    public class BindableList<T> : IReadOnlyList<T>
    {
        public event Action<IReadOnlyList<T>> Changed = delegate { };

        public int Count => Value.Count;
        protected readonly List<T> Values = new();
        public IReadOnlyList<T> Value => Values;

        public T this[int index] => Value[index];

        public void Add(T item)
        {
            Values.Add(item);
            RaiseChangedEvent();
        }
        public void AddRange(IEnumerable<T> items)
        {
            Values.AddRange(items);
            RaiseChangedEvent();
        }
        public void SetRange(IEnumerable<T> items)
        {
            Values.Clear();
            Values.AddRange(items);
            RaiseChangedEvent();
        }
        public bool Remove(T item)
        {
            var removed = Values.Remove(item);
            RaiseChangedEvent();

            return removed;
        }
        public void Clear()
        {
            Values.Clear();
            RaiseChangedEvent();
        }

        public void RaiseChangedEvent() => Changed(Value);
        public void SubscribeChanged(Action<IReadOnlyList<T>> action, bool invokeImmediately = false)
        {
            Changed += action;
            if (invokeImmediately) action(Value);
        }

        List<T>.Enumerator GetEnumerator() => Values.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}