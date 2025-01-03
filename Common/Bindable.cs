using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public interface IBindable
    {
        JToken AsJson(JsonSerializer? serializer);
        void LoadFromJson(JToken json, JsonSerializer? serializer);

        void SubscribeChanged(Action action, bool executeImmediately = false);
    }
    public interface IReadOnlyBindable<out T> : IBindable
    {
        event Action? Changed;
        T Value { get; }

        IReadOnlyBindable<T> GetBoundCopy();
    }
    public interface IBindable<T> : IReadOnlyBindable<T>
    {
        new T Value { get; set; }
    }

    [JsonObject, JsonConverter(typeof(BindableJsonConverter))]
    public abstract class BindableBase<T> : IReadOnlyBindable<T>
    {
        public event Action? Changed;
        readonly List<WeakReference<BindableBase<T>>> References = new();

        public T Value { get => _Value; protected set => InternalSet(value, this); }
        T _Value;

        protected BindableBase(T defval) => _Value = defval;

        public void SubscribeChanged(Action action, bool executeImmediately = false)
        {
            Changed += action;
            if (executeImmediately) TriggerValueChanged();
        }
        public void TriggerValueChanged() => InternalSet(Value, this);

        void InternalSet(T value, BindableBase<T> eventSource)
        {
            _Value = value;
            Changed?.Invoke();

            // TODO: remove weakrefs
            foreach (var weak in References)
                if (weak.TryGetTarget(out var obj) && obj != eventSource)
                    obj.InternalSet(value, this);
        }

        public void Bind(BindableBase<T> other)
        {
            this.References.Add(new(other));
            other.References.Add(new(this));
            this.CopyValueFrom(other);
        }

        IReadOnlyBindable<T> IReadOnlyBindable<T>.GetBoundCopy() => GetBoundCopy();
        public virtual BindableBase<T> GetBoundCopy()
        {
            var obj = (BindableBase<T>) (
                Activator.CreateInstance(GetType(), default(T))
                ?? Activator.CreateInstance(GetType())
                ?? throw new Exception($"A valid {GetType().Name} constructor was not found")
            );

            obj.Bind(this);
            return obj;
        }

        public void Execute(Action action)
        {
            action();
            TriggerValueChanged();
        }

        [OnDeserialized] void OnDeserializing(StreamingContext _) => TriggerValueChanged();
        public virtual JToken AsJson(JsonSerializer? serializer) => Value is null ? JValue.CreateNull() : JToken.FromObject(Value!, serializer ?? JsonSettings.Default);
        public virtual void LoadFromJson(JToken json, JsonSerializer? serializer) => Value = json.ToObject<T>(serializer ?? JsonSettings.Default)!;
        protected virtual void CopyValueFrom(BindableBase<T> other) => Value = other.Value;
    }
    class BindableJsonConverter : JsonConverter<IBindable>
    {
        public override IBindable? ReadJson(JsonReader reader, Type objectType, IBindable? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (existingValue is null) throw new NotImplementedException("Non-populating deserializing IBindables is not supported yet");

            existingValue.LoadFromJson(JToken.Load(reader), LocalApi.JsonSerializerWithType);
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, IBindable? value, JsonSerializer serializer) =>
            (value?.AsJson(serializer) ?? JValue.CreateNull()).WriteTo(writer);
    }

    public class Bindable<T> : BindableBase<T>, IBindable<T>
    {
        public new T Value { get => base.Value; set => base.Value = value; }

        public Bindable(T defval = default!) : base(defval) { }

        public override Bindable<T> GetBoundCopy() => (Bindable<T>) base.GetBoundCopy();
    }

    public interface IReadOnlyBindableCollection<out T> : IBindable, IReadOnlyCollection<T>
    {
        IReadOnlyBindableCollection<T> GetBoundCopy();
    }
    public class BindableList<T> : BindableBase<IReadOnlyList<T>>, IReadOnlyBindable<IReadOnlyList<T>>, IReadOnlyBindableCollection<T>, IReadOnlyList<T>
    {
        public int Count => Value.Count;
        protected new List<T> Value => (List<T>) base.Value;

        public BindableList(IEnumerable<T>? values = null) : base(new List<T>())
        {
            if (values is not null)
                Value.AddRange(values);
        }

        public T this[int index] { get => Value[index]; set => Execute(() => Value[index] = value); }

        public void Add(T value) => Execute(() => Value.Add(value));
        public void AddRange(IEnumerable<T> values) => Execute(() => Value.AddRange(values));
        public void SetRange(IEnumerable<T> values) => Execute(() => { Value.Clear(); Value.AddRange(values); });
        public void Remove(T value) => Execute(() => Value.Remove(value));
        public void Clear() => Execute(Value.Clear);

        public override JToken AsJson(JsonSerializer? serializer) => JToken.FromObject(Value.ToArray(), serializer ?? JsonSettings.Default);
        public override void LoadFromJson(JToken json, JsonSerializer? serializer) => SetRange(json.ToObject<T[]>(serializer ?? JsonSettings.Default)!);
        protected override void CopyValueFrom(BindableBase<IReadOnlyList<T>> other) => SetRange(other.Value);
        public override BindableList<T> GetBoundCopy() => (BindableList<T>) base.GetBoundCopy();

        public IEnumerator<T> GetEnumerator() => Value.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IReadOnlyBindableCollection<T> IReadOnlyBindableCollection<T>.GetBoundCopy() => GetBoundCopy();
        void IBindable.SubscribeChanged(Action action, bool executeImmediately) => SubscribeChanged(action, executeImmediately);
    }
    public class BindableDictionary<TKey, TValue> : BindableBase<IReadOnlyDictionary<TKey, TValue>>, IReadOnlyBindable<IReadOnlyDictionary<TKey, TValue>>,
        IReadOnlyBindableCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        public int Count => Value.Count;
        protected new Dictionary<TKey, TValue> Value => (Dictionary<TKey, TValue>) base.Value;

        public IEnumerable<TKey> Keys => Value.Keys;
        public IEnumerable<TValue> Values => Value.Values;

        public BindableDictionary(IEnumerable<KeyValuePair<TKey, TValue>>? values = null) : base(new Dictionary<TKey, TValue>())
        {
            if (values is not null)
                foreach (var (key, value) in values)
                    Value.Add(key, value);
        }

        public TValue this[TKey key] { get => Value[key]; set => Execute(() => Value[key] = value); }
        public bool ContainsKey(TKey key) => Value.ContainsKey(key);
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Value.TryGetValue(key, out value);

        public void Add(TKey key, TValue value) => Execute(() => Value.Add(key, value));
        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> values) => Execute(() => AddRangeInternal(values));
        void AddRangeInternal(IEnumerable<KeyValuePair<TKey, TValue>> values)
        {
            foreach (var (key, value) in values)
                Value.Add(key, value);
        }
        public void SetRange(IEnumerable<KeyValuePair<TKey, TValue>> values) => Execute(() => { Value.Clear(); AddRangeInternal(values); });
        public void Remove(TKey key) => Execute(() => Value.Remove(key));
        public void Clear() => Execute(Value.Clear);

        public override void LoadFromJson(JToken json, JsonSerializer? serializer) => SetRange(json.ToObject<Dictionary<TKey, TValue>>(LocalApi.JsonSerializerWithType)!);
        protected override void CopyValueFrom(BindableBase<IReadOnlyDictionary<TKey, TValue>> other) => SetRange(other.Value);
        IReadOnlyBindableCollection<KeyValuePair<TKey, TValue>> IReadOnlyBindableCollection<KeyValuePair<TKey, TValue>>.GetBoundCopy() => GetBoundCopy();
        public override BindableDictionary<TKey, TValue> GetBoundCopy() => (BindableDictionary<TKey, TValue>) base.GetBoundCopy();

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Value.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}