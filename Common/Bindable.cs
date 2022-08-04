using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public delegate void ChangedDelegate<T>(T oldv, T newv);

    public interface IBindable { }
    public interface IReadOnlyBindable<T> : IBindable
    {
        public event ChangedDelegate<T> Changed;
        T Value { get; }

        void SubscribeChanged(ChangedDelegate<T> action, bool invokeImmediately = false);
        IReadOnlyBindable<T> GetBoundCopy();
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

        public Bindable(T defaultValue = default!)
        {
            _Value = defaultValue;
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

        IReadOnlyBindable<T> IReadOnlyBindable<T>.GetBoundCopy() => GetBoundCopy();
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

    public interface ICollectionBindable
    {
        public System.Collections.IEnumerable Value { get; }

        void Execute(Action action);
        void Clear();
    }
    [JsonObject, JsonConverter(typeof(CollectionBindableJsonConverter))]
    public abstract class CollectionBindable<TCollection> : ICollectionBindable where TCollection : System.Collections.IEnumerable
    {
        System.Collections.IEnumerable ICollectionBindable.Value => Value;

        bool EventEnabled = true;
        public event Action<TCollection> Changed = delegate { };

        public abstract TCollection Value { get; }

        public void RaiseChangedEvent()
        {
            if (EventEnabled)
                Changed(Value);
        }
        public void SubscribeChanged(Action<TCollection> action, bool invokeImmediately = false)
        {
            Changed += action;
            if (invokeImmediately) action(Value);
        }

        public void Execute(Action action)
        {
            EventEnabled = false;
            using (var _ = new FuncDispose(() => EventEnabled = true))
                action();

            RaiseChangedEvent();
        }

        public abstract void Clear();

        [OnDeserialized]
        void OnDeserializing(StreamingContext _) => RaiseChangedEvent();
    }
    public class BindableList<T> : CollectionBindable<IReadOnlyList<T>>, IReadOnlyList<T>
    {
        public int Count => Value.Count;
        protected readonly List<T> Values = new();
        public override IReadOnlyList<T> Value => Values;

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
        public override void Clear()
        {
            Values.Clear();
            RaiseChangedEvent();
        }

        List<T>.Enumerator GetEnumerator() => Values.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class BindableDictionary<TKey, TValue> : CollectionBindable<IReadOnlyDictionary<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => ((IReadOnlyDictionary<TKey, TValue>) Dictionary).Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ((IReadOnlyDictionary<TKey, TValue>) Dictionary).Values;
        public Dictionary<TKey, TValue>.KeyCollection Keys => Dictionary.Keys;
        public Dictionary<TKey, TValue>.ValueCollection Values => Dictionary.Values;

        public int Count => Value.Count;
        protected readonly Dictionary<TKey, TValue> Dictionary = new();
        public override IReadOnlyDictionary<TKey, TValue> Value => Dictionary;

        public TValue this[TKey key]
        {
            get => Value[key];
            set
            {
                Dictionary[key] = value;
                RaiseChangedEvent();
            }
        }

        public void Add(TKey key, TValue value)
        {
            Dictionary.Add(key, value);
            RaiseChangedEvent();
        }
        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var (key, value) in items)
                Dictionary.Add(key, value);

            RaiseChangedEvent();
        }
        public void SetRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            Dictionary.Clear();
            foreach (var (key, value) in items)
                Dictionary.Add(key, value);

            RaiseChangedEvent();
        }
        public bool Remove(TKey key)
        {
            var removed = Dictionary.Remove(key);
            RaiseChangedEvent();

            return removed;
        }
        public override void Clear()
        {
            Dictionary.Clear();
            RaiseChangedEvent();
        }

        public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);
        public bool TryGetValue(TKey key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out TValue value) => Dictionary.TryGetValue(key, out value);

        Dictionary<TKey, TValue>.Enumerator GetEnumerator() => Dictionary.GetEnumerator();
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }


    class CollectionBindableJsonConverter : JsonConverter<ICollectionBindable>
    {
        public override ICollectionBindable? ReadJson(JsonReader reader, Type objectType, ICollectionBindable? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (existingValue is null) throw new NotImplementedException("Deserializing AE is not yet supported");

            existingValue.Execute(() =>
            {
                existingValue.Clear();
                serializer.Populate(reader, existingValue.Value);
            });

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, ICollectionBindable? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            JToken.FromObject(value.Value).WriteTo(writer);
        }
    }
}